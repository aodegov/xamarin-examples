﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using Autofac;
using GitHubApiStatus;
using GitTrends.Mobile.Common;
using GitTrends.Mobile.Common.Constants;
using GitTrends.Shared;
using Refit;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace GitTrends
{
	public class RepositoryViewModel : BaseViewModel
	{
		readonly static WeakEventManager<PullToRefreshFailedEventArgs> _pullToRefreshFailedEventManager = new();

		readonly GitHubUserService _gitHubUserService;
		readonly RepositoryDatabase _repositoryDatabase;
		readonly DeepLinkingService _deepLinkingService;
		readonly GitHubApiV3Service _gitHubApiV3Service;
		readonly ImageCachingService _imageCachingService;
		readonly MobileSortingService _mobileSortingService;
		readonly GitHubApiStatusService _gitHubApiStatusService;
		readonly BackgroundFetchService _backgroundFetchService;
		readonly GitHubGraphQLApiService _gitHubGraphQLApiService;
		readonly GitHubAuthenticationService _gitHubAuthenticationService;
		readonly GitHubApiRepositoriesService _gitHubApiRepositoriesService;

		bool _isRefreshing;
		string _titleText = string.Empty;
		string _searchBarText = string.Empty;
		string _totalButtonText = string.Empty;
		string _emptyDataViewTitle = string.Empty;
		string _emptyDataViewDescription = string.Empty;

		IReadOnlyList<Repository> _repositoryList = Array.Empty<Repository>();
		IReadOnlyList<Repository> _visibleRepositoryList = Array.Empty<Repository>();

		public RepositoryViewModel(IMainThread mainThread,
									IAnalyticsService analyticsService,
									GitHubUserService gitHubUserService,
									RepositoryDatabase repositoryDatabase,
									DeepLinkingService deepLinkingService,
									GitHubApiV3Service gitHubApiV3Service,
									ImageCachingService imageCachingService,
									MobileSortingService mobileSortingService,
									GitHubApiStatusService gitHubApiStatusService,
									BackgroundFetchService backgroundFetchService,
									GitHubGraphQLApiService gitHubGraphQLApiService,
									GitHubAuthenticationService gitHubAuthenticationService,
									GitHubApiRepositoriesService gitHubApiRepositoriesService) : base(analyticsService, mainThread)
		{
			LanguageService.PreferredLanguageChanged += HandlePreferredLanguageChanged;
			TrendsViewModel.RepositorySavedToDatabase += HandleTrendsViewModelRepositorySavedToDatabase;
			BackgroundFetchService.ScheduleRetryRepositoriesStarsCompleted += HandleScheduleRetryRepositoriesStarsCompleted;
			BackgroundFetchService.ScheduleRetryRepositoriesViewsClonesStarsCompleted += HandleScheduleRetryRepositoriesViewsClonesStarsCompleted;

			UpdateText();

			_gitHubUserService = gitHubUserService;
			_repositoryDatabase = repositoryDatabase;
			_deepLinkingService = deepLinkingService;
			_gitHubApiV3Service = gitHubApiV3Service;
			_imageCachingService = imageCachingService;
			_mobileSortingService = mobileSortingService;
			_gitHubApiStatusService = gitHubApiStatusService;
			_backgroundFetchService = backgroundFetchService;
			_gitHubGraphQLApiService = gitHubGraphQLApiService;
			_gitHubAuthenticationService = gitHubAuthenticationService;
			_gitHubApiRepositoriesService = gitHubApiRepositoriesService;

			RefreshState = RefreshState.Uninitialized;

			FilterRepositoriesCommand = new Command<string>(SetSearchBarText);
			SortRepositoriesCommand = new Command<SortingOption>(ExecuteSortRepositoriesCommand);

			PullToRefreshCommand = new AsyncCommand(() => ExecutePullToRefreshCommand(gitHubUserService.Alias));

			ToggleIsFavoriteCommand = new AsyncCommand<Repository>(repository => repository switch
			{
				null => Task.CompletedTask,
				_ => ExecuteToggleIsFavoriteCommand(repository)
			});

			NavigateToRepositoryWebsiteCommand = new AsyncCommand<Repository>(repository => repository switch
			{
				null => Task.CompletedTask,
				_ => ExecuteNavigateToRepositoryWebsiteCommand(repository)
			});

			NotificationService.SortingOptionRequested += HandleSortingOptionRequested;

			GitHubApiRepositoriesService.RepositoryUriNotFound += HandleRepositoryUriNotFound;

			GitHubAuthenticationService.DemoUserActivated += HandleDemoUserActivated;
			GitHubAuthenticationService.LoggedOut += HandleGitHubAuthenticationServiceLoggedOut;
			GitHubAuthenticationService.AuthorizeSessionCompleted += HandleAuthorizeSessionCompleted;
			GitHubUserService.ShouldIncludeOrganizationsChanged += HandleShouldIncludeOrganizationsChanged;
		}

		public static event EventHandler<PullToRefreshFailedEventArgs> PullToRefreshFailed
		{
			add => _pullToRefreshFailedEventManager.AddEventHandler(value);
			remove => _pullToRefreshFailedEventManager.RemoveEventHandler(value);
		}

		public ICommand SortRepositoriesCommand { get; }
		public ICommand FilterRepositoriesCommand { get; }
		public IAsyncCommand PullToRefreshCommand { get; }
		public AsyncCommand<Repository> ToggleIsFavoriteCommand { get; }
		public AsyncCommand<Repository> NavigateToRepositoryWebsiteCommand { get; }

		public IReadOnlyList<Repository> VisibleRepositoryList
		{
			get => _visibleRepositoryList;
			set => SetProperty(ref _visibleRepositoryList, value);
		}

		public string EmptyDataViewTitle
		{
			get => _emptyDataViewTitle;
			set => SetProperty(ref _emptyDataViewTitle, value);
		}

		public string EmptyDataViewDescription
		{
			get => _emptyDataViewDescription;
			set => SetProperty(ref _emptyDataViewDescription, value);
		}

		public bool IsRefreshing
		{
			get => _isRefreshing;
			set => SetProperty(ref _isRefreshing, value);
		}

		public string TotalButtonText
		{
			get => _totalButtonText;
			set => SetProperty(ref _totalButtonText, value);
		}

		public string TitleText
		{
			get => _titleText;
			set => SetProperty(ref _titleText, value);
		}

		RefreshState RefreshState
		{
			set
			{
				EmptyDataViewTitle = EmptyDataViewService.GetRepositoryTitleText(value, !_repositoryList.Any());
				EmptyDataViewDescription = EmptyDataViewService.GetRepositoryDescriptionText(value, !_repositoryList.Any());
			}
		}

		static IEnumerable<Repository> GetRepositoriesFilteredBySearchBar(in IEnumerable<Repository> repositories, string searchBarText)
		{
			if (string.IsNullOrWhiteSpace(searchBarText))
				return repositories;

			return repositories.Where(x => x.Name.Contains(searchBarText, StringComparison.OrdinalIgnoreCase));
		}

		async Task ExecutePullToRefreshCommand(string repositoryOwner)
		{
			const int minimumBatchCount = 100;

			HttpResponseMessage? finalResponse = null;
			IReadOnlyList<Repository>? repositoriesFromDatabase = null;
			var saveCompletedRepositoryToDatabaseTaskList = new List<Task>();

			var cancellationTokenSource = new CancellationTokenSource();
			GitHubAuthenticationService.LoggedOut += HandleLoggedOut;
			GitHubAuthenticationService.AuthorizeSessionStarted += HandleAuthorizeSessionStarted;
			GitHubUserService.ShouldIncludeOrganizationsChanged += HandleShouldIncludeOrganizationsChanged;

			AnalyticsService.Track("Refresh Triggered", "Sorting Option", _mobileSortingService.CurrentOption.ToString());

			var repositoriesFromDatabaseTask = _repositoryDatabase.GetRepositories();

			try
			{
				#region Get Visible RepositoryList Data in Foreground
				var favoriteRepositoryUrls = await _repositoryDatabase.GetFavoritesUrls().ConfigureAwait(false);

				var repositoryList = new List<Repository>();
				await foreach (var repository in _gitHubGraphQLApiService.GetRepositories(repositoryOwner, cancellationTokenSource.Token).ConfigureAwait(false))
				{
					if (favoriteRepositoryUrls.Contains(repository.Url))
						repositoryList.Add(repository with { IsFavorite = true });
					else
						repositoryList.Add(repository);

					//Batch the VisibleRepositoryList Updates to avoid overworking the UI Thread
					if (!_gitHubUserService.IsDemoUser && repositoryList.Count > minimumBatchCount)
					{
						//Only display the first update to avoid unncessary work on the UIThread
						var shouldUpdateVisibleRepositoryList = !VisibleRepositoryList.Any() || repositoryList.Count >= minimumBatchCount;
						AddRepositoriesToCollection(repositoryList, _searchBarText, shouldUpdateVisibleRepositoryList);
						repositoryList.Clear();
					}
				}

				//Add Remaining Repositories to _repositoryList
				AddRepositoriesToCollection(repositoryList, _searchBarText);

				repositoriesFromDatabase = await repositoriesFromDatabaseTask.ConfigureAwait(false);

				var repositoriesFromDatabaseThatDontRequireUpdating = repositoriesFromDatabase.Where(x => x.ContainsViewsClonesData // Ensure the repository contains data for Views + Clones
																											&& x.DataDownloadedAt >= DateTimeOffset.Now.Subtract(CachedDataConstants.ViewsClonesCacheLifeSpan) // Cached repositories that have been updated in the past 12 hours				
																											&& _repositoryList.Any(y => y.Url == x.Url)); // Ensure was retrieved from GitHub)

				AddRepositoriesToCollection(repositoriesFromDatabaseThatDontRequireUpdating, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesData);

				var viewsClonesRepsitoriesList = new List<Repository>();
				await foreach (var retrievedRepositoryWithViewsAndClonesData in _gitHubApiRepositoriesService.UpdateRepositoriesWithViewsAndClonesData(_repositoryList.Where(x => !x.ContainsViewsClonesData), cancellationTokenSource.Token).ConfigureAwait(false))
				{
					viewsClonesRepsitoriesList.Add(retrievedRepositoryWithViewsAndClonesData);

					if (!_gitHubUserService.IsDemoUser)
					{
						//Batch the VisibleRepositoryList Updates to avoid overworking the UI Thread
						if (viewsClonesRepsitoriesList.Count > minimumBatchCount)
						{
							AddRepositoriesToCollection(viewsClonesRepsitoriesList, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesData);
							viewsClonesRepsitoriesList.Clear();
						}
					}
				}

				//Add Remaining Repositories to VisibleRepositoryList
				AddRepositoriesToCollection(viewsClonesRepsitoriesList, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesData);
				#endregion

				IsRefreshing = false;

				#region Get StarGazer Data in Background
				// The StarGazer data can be gathered in the background because the data only appears if the user navigates to the StarsTrendsPage
				// The data is gathered in the background to optimize the Pull-To-Refresh time visible to the user
				repositoriesFromDatabaseThatDontRequireUpdating = repositoriesFromDatabase.Where(x => x.ContainsViewsClonesStarsData // Ensure the repository contains data for Views + Clones + Stars
																											&& x.DataDownloadedAt >= DateTimeOffset.Now.Subtract(CachedDataConstants.StarsDataCacheLifeSpan) // Cached repositories that have been updated in the past 12 hours				
																											&& _repositoryList.Any(y => y.Url == x.Url)); // Ensure was retrieved from GitHub)

				AddRepositoriesToCollection(repositoriesFromDatabaseThatDontRequireUpdating, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesStarsData);


				var repositoriesWithoutStarsDataAndOver1000Stars = _repositoryList.Where(x => !x.ContainsViewsClonesStarsData && x.StarCount > 1000);
				var repositoriesWithoutStarsDataAndLessThan1000Stars = _repositoryList.Where(x => !x.ContainsViewsClonesStarsData && x.StarCount <= 1000);

				// Fetch Stars Data in Background for Repositories Containing Over 1000 Stars
				// GitHub API limits us to 100 StarGazers per Request, meaning that a repository with 24K Stars requires 240 round-trips from GitTrends to GitHub's servers to aggregate the data
				// This data is not displayed in the Repository Page
				foreach (var repository in repositoriesWithoutStarsDataAndOver1000Stars)
					_backgroundFetchService.TryScheduleRetryRepositoriesStars(repository);

				var starredRepositoriesList = new List<Repository>();
				await foreach (var retrievedRepositoryWithStarsData in _gitHubApiRepositoriesService.UpdateRepositoriesWithStarsData(repositoriesWithoutStarsDataAndLessThan1000Stars, cancellationTokenSource.Token).ConfigureAwait(false))
				{
					starredRepositoriesList.Add(retrievedRepositoryWithStarsData);

					if (!_gitHubUserService.IsDemoUser)
					{
						var saveRepositoryToDatabaseTask = _repositoryDatabase.SaveRepository(retrievedRepositoryWithStarsData);
						saveCompletedRepositoryToDatabaseTaskList.Add(saveRepositoryToDatabaseTask);

						//Batch the VisibleRepositoryList Updates to avoid overworking the UI Thread
						if (starredRepositoriesList.Count > minimumBatchCount)
						{
							AddRepositoriesToCollection(starredRepositoriesList, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesStarsData);
							starredRepositoriesList.Clear();
						}
					}
				}

				AddRepositoriesToCollection(starredRepositoriesList, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesStarsData);
				#endregion

				if (!_gitHubUserService.IsDemoUser)
				{
					//Rate Limiting may cause some data to not return successfully from the GitHub API
					var missingRepositories = _gitHubUserService.ShouldIncludeOrganizations switch
					{
						true => getDistictRepositories(_repositoryList, repositoriesFromDatabase, x => x.ContainsViewsClonesStarsData),
						false => getDistictRepositories(_repositoryList, repositoriesFromDatabase, x => x.ContainsViewsClonesStarsData && x.OwnerLogin == _gitHubUserService.Alias)
					};

					AddRepositoriesToCollection(missingRepositories, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesData);

					//Call EnsureSuccessStatusCode to confirm the above API calls executed successfully
					finalResponse = await _gitHubApiV3Service.GetGitHubApiResponse(cancellationTokenSource.Token).ConfigureAwait(false);
					finalResponse.EnsureSuccessStatusCode();
				}

				RefreshState = RefreshState.Succeeded;
			}
			catch (Exception e) when ((e is ApiException { StatusCode: HttpStatusCode.Unauthorized })
										|| (e is HttpRequestException && finalResponse?.StatusCode is HttpStatusCode.Unauthorized))
			{
				var loginExpiredEventArgs = new LoginExpiredPullToRefreshEventArgs();

				OnPullToRefreshFailed(new LoginExpiredPullToRefreshEventArgs());

				await _gitHubAuthenticationService.LogOut().ConfigureAwait(false);
				await _repositoryDatabase.DeleteAllData().ConfigureAwait(false);

				SetRepositoriesCollection(Array.Empty<Repository>(), _searchBarText);
			}
			catch (Exception e) when (_gitHubApiStatusService.IsAbuseRateLimit(e, out var retryTimeSpan)
										|| (e is HttpRequestException && finalResponse is not null && _gitHubApiStatusService.IsAbuseRateLimit(finalResponse.Headers, out retryTimeSpan)))
			{
				repositoriesFromDatabase ??= await repositoriesFromDatabaseTask.ConfigureAwait(false);

				//Rate Limiting may cause some data to not return successfully from the GitHub API
				var missingRepositories = _gitHubUserService.ShouldIncludeOrganizations switch
				{
					true => getDistictRepositories(_repositoryList, repositoriesFromDatabase),
					false => getDistictRepositories(_repositoryList, repositoriesFromDatabase, x => x.OwnerLogin == _gitHubUserService.Alias)
				};

				AddRepositoriesToCollection(missingRepositories, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesData);

				foreach (var repositoryToUpdate in _repositoryList.Where(x => !x.ContainsViewsClonesStarsData // Ensure the repository contains data for Views + Clones
																				 && x.DataDownloadedAt < DateTimeOffset.Now.Subtract(TimeSpan.FromHours(12)))) // Cached repositories that have been updated in the past 12 hours
				{
					_backgroundFetchService.TryScheduleRetryRepositoriesStars(repositoryToUpdate, retryTimeSpan.Value);
				}

				var abuseLimit = new AbuseLimitPullToRefreshEventArgs(retryTimeSpan.Value, VisibleRepositoryList.Any());
				OnPullToRefreshFailed(abuseLimit);
			}
			catch (Exception e) when (_gitHubApiStatusService.HasReachedMaximumApiCallLimit(e)
										|| (e is HttpRequestException && finalResponse is not null && finalResponse.Headers.DoesContainGitHubRateLimitHeader() && _gitHubApiStatusService.HasReachedMaximimApiCallLimit(finalResponse.Headers)))
			{
				var responseHeaders = e switch
				{
					ApiException exception => exception.Headers,
					GraphQLException graphQLException => graphQLException.ResponseHeaders,
					HttpRequestException when finalResponse is not null => finalResponse.Headers,
					_ => throw new NotSupportedException()
				};

				var maximimApiRequestsReachedEventArgs = new MaximumApiRequestsReachedEventArgs(_gitHubApiStatusService.GetRateLimitResetDateTime(responseHeaders));
				OnPullToRefreshFailed(maximimApiRequestsReachedEventArgs);

				SetRepositoriesCollection(Array.Empty<Repository>(), _searchBarText);
			}
			catch (Exception e)
			{
				AnalyticsService.Report(e);

				if (repositoriesFromDatabase is null)
					repositoriesFromDatabase = await repositoriesFromDatabaseTask.ConfigureAwait(false);

				SetRepositoriesCollection(repositoriesFromDatabase, _searchBarText);

				if (repositoriesFromDatabase.Any())
				{
					var dataDownloadedAt = repositoriesFromDatabase.Max(x => x.DataDownloadedAt);
					OnPullToRefreshFailed(new ErrorPullToRefreshEventArgs($"{RepositoryPageConstants.DisplayingDataFrom} {dataDownloadedAt.ToLocalTime():dd MMMM @ HH:mm}"));
				}
				else
				{
					OnPullToRefreshFailed(new ErrorPullToRefreshEventArgs(RepositoryPageConstants.CheckInternetConnectionTryAgain));
				}
			}
			finally
			{
				GitHubAuthenticationService.LoggedOut -= HandleLoggedOut;
				GitHubAuthenticationService.AuthorizeSessionStarted -= HandleAuthorizeSessionStarted;
				GitHubUserService.ShouldIncludeOrganizationsChanged -= HandleShouldIncludeOrganizationsChanged;

				if (cancellationTokenSource.IsCancellationRequested)
					UpdateListForLoggedOutUser();

				IsRefreshing = false;

				await Task.WhenAll(saveCompletedRepositoryToDatabaseTaskList).ConfigureAwait(false);
			}

			static IReadOnlyList<Repository> getDistictRepositories(in IEnumerable<Repository> repositoriesList1, in IEnumerable<Repository> repositoriesList2, Func<Repository, bool>? filter = null) =>
					repositoriesList1.Concat(repositoriesList2).Where(filter ?? (_ => true)).GroupBy(x => x.Url).Where(g => g.Count() is 1).Select(g => g.First()).ToList();

			void HandleLoggedOut(object sender, EventArgs e) => cancellationTokenSource.Cancel();
			void HandleAuthorizeSessionStarted(object sender, EventArgs e) => cancellationTokenSource.Cancel();
			void HandleShouldIncludeOrganizationsChanged(object sender, bool e) => cancellationTokenSource.Cancel();
		}

		async Task ExecuteToggleIsFavoriteCommand(Repository repository)
		{
			var updatedRepository = repository with
			{
				IsFavorite = repository.IsFavorite.HasValue ? !repository.IsFavorite : true
			};

			AnalyticsService.Track("IsFavorite Toggled", nameof(Repository.IsFavorite), updatedRepository.IsFavorite.ToString());

			var updatedRepositoryList = new List<Repository>(_visibleRepositoryList);
			updatedRepositoryList.Remove(repository);
			updatedRepositoryList.Add(updatedRepository);

			SetRepositoriesCollection(updatedRepositoryList, _searchBarText);

			if (!_gitHubUserService.IsDemoUser)
				await _repositoryDatabase.SaveRepository(updatedRepository).ConfigureAwait(false);
		}

		Task ExecuteNavigateToRepositoryWebsiteCommand(Repository repository)
		{
			AnalyticsService.Track("Open External Repostory Link Tapped", nameof(repository.Url), repository.Url);

			return _deepLinkingService.OpenApp(GitHubConstants.AppScheme, repository.Url, repository.Url);
		}

		void ExecuteSortRepositoriesCommand(SortingOption option)
		{
			if (_mobileSortingService.CurrentOption == option)
				_mobileSortingService.IsReversed = !_mobileSortingService.IsReversed;
			else
				_mobileSortingService.IsReversed = false;

			_mobileSortingService.CurrentOption = option;

			AnalyticsService.Track($"{nameof(SortingOption)} Changed", new Dictionary<string, string>
			{
				{ nameof(MobileSortingService) + nameof(MobileSortingService.CurrentOption), _mobileSortingService.CurrentOption.ToString() },
				{ nameof(MobileSortingService) + nameof(MobileSortingService.IsReversed), _mobileSortingService.IsReversed.ToString() }
			});

			UpdateVisibleRepositoryList(_searchBarText, _mobileSortingService.CurrentOption, _mobileSortingService.IsReversed);
		}

		void SetRepositoriesCollection(in IReadOnlyList<Repository> repositories, in string searchBarText)
		{
			_repositoryList = repositories;

			UpdateVisibleRepositoryList(searchBarText, _mobileSortingService.CurrentOption, _mobileSortingService.IsReversed);
		}

		void AddRepositoriesToCollection(in IEnumerable<Repository> repositories, in string searchBarText, in bool shouldUpdateVisibleRepositoryList = true, Func<Repository, bool>? duplicateRepositoryPriorityFilter = null)
		{
			if (!repositories.Any())
				return;

			duplicateRepositoryPriorityFilter ??= _ => true;

			var updatedRepositoryList = _repositoryList.Concat(repositories);
			_repositoryList = updatedRepositoryList.RemoveForksAndDuplicates(duplicateRepositoryPriorityFilter).ToList();

			if (shouldUpdateVisibleRepositoryList)
				UpdateVisibleRepositoryList(searchBarText, _mobileSortingService.CurrentOption, _mobileSortingService.IsReversed);
		}

		void RemoveRepositoriesFromCollection(in IEnumerable<Repository> repositories, in string searchBarText, in bool shouldUpdateVisibleRepositoryList = true)
		{
			if (!repositories.Any())
				return;

			var updatedRepositoryList = new List<Repository>(_repositoryList);
			foreach (var repositoryToRemove in repositories)
			{
				updatedRepositoryList.Remove(repositoryToRemove);
			}

			_repositoryList = updatedRepositoryList.RemoveForksAndDuplicates(x => x.ContainsViewsClonesStarsData).ToList();

			if (shouldUpdateVisibleRepositoryList)
				UpdateVisibleRepositoryList(searchBarText, _mobileSortingService.CurrentOption, _mobileSortingService.IsReversed);
		}

		void UpdateVisibleRepositoryList(in string searchBarText, in SortingOption sortingOption, in bool isReversed)
		{
			var filteredRepositoryList = GetRepositoriesFilteredBySearchBar(_repositoryList, searchBarText);

			VisibleRepositoryList = MobileSortingService.SortRepositories(filteredRepositoryList, sortingOption, isReversed).ToList();

			_imageCachingService.PreloadRepositoryImages(VisibleRepositoryList).SafeFireAndForget(ex => AnalyticsService.Report(ex));
		}

		void UpdateListForLoggedOutUser()
		{
			_repositoryList = Array.Empty<Repository>();
			UpdateVisibleRepositoryList(string.Empty, _mobileSortingService.CurrentOption, _mobileSortingService.IsReversed);
		}

		void SetSearchBarText(string text)
		{
			if (EqualityComparer<string>.Default.Equals(_searchBarText, text))
				return;

			_searchBarText = text;

			if (_repositoryList.Any())
				UpdateVisibleRepositoryList(_searchBarText, _mobileSortingService.CurrentOption, _mobileSortingService.IsReversed);
		}

		void HandlePreferredLanguageChanged(object sender, string? e) => UpdateText();

		void UpdateText()
		{
			TitleText = PageTitles.RepositoryPage;
			TotalButtonText = RepositoryPageConstants.TOTAL;
		}

		// Work-around because ContentPage.OnAppearing does not fire after `ContentPage.PushModalAsync()`
		// Fixed in Xamarin.Forms v5.0.0-sr10 https://github.com/xamarin/Xamarin.Forms/commit/103ef3df7063e42851288c6977567193a74eaaaf
		void HandleAuthorizeSessionCompleted(object sender, AuthorizeSessionCompletedEventArgs e) => IsRefreshing |= e.IsSessionAuthorized;

		async void HandleShouldIncludeOrganizationsChanged(object sender, bool e)
		{
			UpdateListForLoggedOutUser();
			await _repositoryDatabase.DeleteAllData().ConfigureAwait(false);
		}

		void HandleRepositoryUriNotFound(object sender, Uri e)
		{
			var repositoriesToRemove = _repositoryList.Where(x => x.Url == e.ToString());
			RemoveRepositoriesFromCollection(repositoriesToRemove, _searchBarText);
		}

		void HandleDemoUserActivated(object sender, EventArgs e) => IsRefreshing = true;

		void HandleGitHubAuthenticationServiceLoggedOut(object sender, EventArgs e) => UpdateListForLoggedOutUser();

		void HandleSortingOptionRequested(object sender, SortingOption sortingOption) => SortRepositoriesCommand.Execute(sortingOption);

		void OnPullToRefreshFailed(PullToRefreshFailedEventArgs pullToRefreshFailedEventArgs)
		{
			RefreshState = pullToRefreshFailedEventArgs switch
			{
				ErrorPullToRefreshEventArgs => RefreshState.Error,
				AbuseLimitPullToRefreshEventArgs => RefreshState.AbuseLimit,
				LoginExpiredPullToRefreshEventArgs => RefreshState.LoginExpired,
				MaximumApiRequestsReachedEventArgs => RefreshState.MaximumApiLimit,
				_ => throw new NotSupportedException()
			};

			_pullToRefreshFailedEventManager.RaiseEvent(this, pullToRefreshFailedEventArgs, nameof(PullToRefreshFailed));
		}

		void HandleScheduleRetryRepositoriesStarsCompleted(object sender, Repository e) => AddRepositoriesToCollection(new Repository[] { e }, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesStarsData);
		void HandleTrendsViewModelRepositorySavedToDatabase(object sender, Repository e) => AddRepositoriesToCollection(new Repository[] { e }, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesStarsData);
		void HandleScheduleRetryRepositoriesViewsClonesStarsCompleted(object sender, Repository e) => AddRepositoriesToCollection(new Repository[] { e }, _searchBarText, duplicateRepositoryPriorityFilter: x => x.ContainsViewsClonesStarsData);
	}
}
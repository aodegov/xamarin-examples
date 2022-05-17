﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using GitHubApiStatus;
using GitTrends.Shared;
using Refit;

namespace GitTrends
{
	public class GitHubApiRepositoriesService
	{
		readonly static WeakEventManager<(Repository Repository, TimeSpan RetryTimeSpan)> _abuseRateLimitFoundEventManager = new();
		readonly static WeakEventManager<Uri> _repositoryUriNotFoundEventManager = new();

		readonly FavIconService _favIconService;
		readonly IAnalyticsService _analyticsService;
		readonly GitHubUserService _gitHubUserService;
		readonly GitHubApiV3Service _gitHubApiV3Service;
		readonly ReferringSitesDatabase _referringSitesDatabase;
		readonly GitHubApiStatusService _gitHubApiStatusService;
		readonly GitHubGraphQLApiService _gitHubGraphQLApiService;

		public GitHubApiRepositoriesService(FavIconService favIconService,
											IAnalyticsService analyticsService,
											GitHubUserService gitHubUserService,
											GitHubApiV3Service gitHubApiV3Service,
											ReferringSitesDatabase referringSitesDatabase,
											GitHubApiStatusService gitHubApiStatusService,
											GitHubGraphQLApiService gitHubGraphQLApiService)
		{
			_favIconService = favIconService;
			_analyticsService = analyticsService;
			_gitHubUserService = gitHubUserService;
			_gitHubApiV3Service = gitHubApiV3Service;
			_referringSitesDatabase = referringSitesDatabase;
			_gitHubApiStatusService = gitHubApiStatusService;
			_gitHubGraphQLApiService = gitHubGraphQLApiService;
		}

		public static event EventHandler<Uri> RepositoryUriNotFound
		{
			add => _repositoryUriNotFoundEventManager.AddEventHandler(value);
			remove => _repositoryUriNotFoundEventManager.RemoveEventHandler(value);
		}

		public static event EventHandler<(Repository Repository, TimeSpan RetryTimeSpan)> AbuseRateLimitFound_GetReferringSites
		{
			add => _abuseRateLimitFoundEventManager.AddEventHandler(value);
			remove => _abuseRateLimitFoundEventManager.RemoveEventHandler(value);
		}

		public static event EventHandler<(Repository Repository, TimeSpan RetryTimeSpan)> AbuseRateLimitFound_UpdateRepositoriesWithViewsClonesAndStarsData
		{
			add => _abuseRateLimitFoundEventManager.AddEventHandler(value);
			remove => _abuseRateLimitFoundEventManager.RemoveEventHandler(value);
		}

		public async Task<IReadOnlyList<ReferringSiteModel>> GetReferringSites(Repository repository, CancellationToken cancellationToken)
		{
			try
			{
				return await _gitHubApiV3Service.GetReferringSites(repository.OwnerLogin, repository.Name, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e) when (_gitHubApiStatusService.IsAbuseRateLimit(e, out var retryDelay))
			{
				OnAbuseRateLimitFound_GetReferringSites(repository, retryDelay.Value);
				throw;
			}
		}

		public async IAsyncEnumerable<MobileReferringSiteModel> GetMobileReferringSites(IEnumerable<ReferringSiteModel> referringSites, string repositoryUrl, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var favIconTaskList = referringSites.Select(x => setFavIcon(_referringSitesDatabase, x, repositoryUrl, cancellationToken)).ToList();

			while (favIconTaskList.Any())
			{
				var completedFavIconTask = await Task.WhenAny(favIconTaskList).ConfigureAwait(false);
				favIconTaskList.Remove(completedFavIconTask);

				var mobileReferringSiteModel = await completedFavIconTask.ConfigureAwait(false);
				yield return mobileReferringSiteModel;
			}

			async Task<MobileReferringSiteModel> setFavIcon(ReferringSitesDatabase referringSitesDatabase, ReferringSiteModel referringSiteModel, string repositoryUrl, CancellationToken cancellationToken)
			{
				var mobileReferringSiteFromDatabase = await referringSitesDatabase.GetReferringSite(repositoryUrl, referringSiteModel.ReferrerUri).ConfigureAwait(false);

				if (mobileReferringSiteFromDatabase != null && isFavIconValid(mobileReferringSiteFromDatabase))
					return mobileReferringSiteFromDatabase;

				if (referringSiteModel.ReferrerUri != null && referringSiteModel.IsReferrerUriValid)
				{
					var favIcon = await _favIconService.GetFavIconImageSource(referringSiteModel.ReferrerUri, cancellationToken).ConfigureAwait(false);
					return new MobileReferringSiteModel(referringSiteModel, favIcon);
				}
				else
				{
					return new MobileReferringSiteModel(referringSiteModel, FavIconService.DefaultFavIcon);
				}

				static bool isFavIconValid(MobileReferringSiteModel mobileReferringSiteModel) => !string.IsNullOrWhiteSpace(mobileReferringSiteModel.FavIconImageUrl) && mobileReferringSiteModel.DownloadedAt.CompareTo(DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30))) > 0;
			}
		}

		public async IAsyncEnumerable<Repository> UpdateRepositoriesWithViewsAndClonesData(IEnumerable<Repository> repositories, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var getRepositoryStatisticsTaskList = new List<Task<(RepositoryViewsResponseModel?, RepositoryClonesResponseModel?)>>(repositories.Select(x => GetViewsAndClonesStatistics(x, cancellationToken)));

			while (getRepositoryStatisticsTaskList.Any())
			{
				var completedStatisticsTask = await Task.WhenAny(getRepositoryStatisticsTaskList).ConfigureAwait(false);
				getRepositoryStatisticsTaskList.Remove(completedStatisticsTask);

				var (viewsResponse, clonesResponse) = await completedStatisticsTask.ConfigureAwait(false);

				if (viewsResponse is not null
					&& clonesResponse is not null)
				{
					var updatedRepository = repositories.Single(x => x.Name == viewsResponse.RepositoryName) with
					{
						DailyViewsList = viewsResponse.DailyViewsList,
						DailyClonesList = clonesResponse.DailyClonesList
					};

					yield return updatedRepository;
				}
			}
		}

		public async IAsyncEnumerable<Repository> UpdateRepositoriesWithStarsData(IEnumerable<Repository> repositories, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var getRepositoryStatisticsTaskList = new List<Task<(string RepositoryName, StarGazers? StarGazers)>>(repositories.Select(x => GetStarGazersStatistics(x, cancellationToken)));

			while (getRepositoryStatisticsTaskList.Any())
			{
				var completedStatisticsTask = await Task.WhenAny(getRepositoryStatisticsTaskList).ConfigureAwait(false);
				getRepositoryStatisticsTaskList.Remove(completedStatisticsTask);

				var starsResponse = await completedStatisticsTask.ConfigureAwait(false);

				if (starsResponse.StarGazers is not null)
				{
					var updatedRepository = repositories.Single(x => x.Name == starsResponse.RepositoryName) with
					{
						StarredAt = starsResponse.StarGazers.StarredAt.Select(x => x.StarredAt).ToList()
					};

					yield return updatedRepository;
				}
			}
		}

		async Task<(RepositoryViewsResponseModel? ViewsResponse, RepositoryClonesResponseModel? ClonesResponse)> GetViewsAndClonesStatistics(Repository repository, CancellationToken cancellationToken)
		{
			var getViewStatisticsTask = _gitHubApiV3Service.GetRepositoryViewStatistics(repository.OwnerLogin, repository.Name, cancellationToken);
			var getCloneStatisticsTask = _gitHubApiV3Service.GetRepositoryCloneStatistics(repository.OwnerLogin, repository.Name, cancellationToken);

			try
			{
				await Task.WhenAll(getViewStatisticsTask, getCloneStatisticsTask).ConfigureAwait(false);

				return (await getViewStatisticsTask.ConfigureAwait(false),
						await getCloneStatisticsTask.ConfigureAwait(false));
			}
			catch (ApiException e) when (_gitHubApiStatusService.IsAbuseRateLimit(e.Headers, out var timespan))
			{
				OnAbuseRateLimitFound_UpdateRepositoriesWithViewsClonesAndStarsData(repository, timespan.Value);

				return (null, null);
			}
			catch (ApiException e) when (e.StatusCode is System.Net.HttpStatusCode.Forbidden)
			{
				reportException(e);

				return (null, null);
			}
			catch (GraphQLException<StarGazers> e) when (e.ContainsSamlOrganizationAthenticationError(out _))
			{
				reportException(e);

				return (null, null);
			}
			catch (ApiException e) when (e.StatusCode is System.Net.HttpStatusCode.NotFound) // Repository deleted from GitHub but has not yet been deleted from local SQLite Database
			{
				reportException(e);

				OnRepositoryUriNotFound(repository.Url);

				return (null, null);
			}

			void reportException(in Exception e)
			{
				_analyticsService.Report(e, new Dictionary<string, string>
				{
					{ nameof(Repository) + nameof(Repository.Name), repository.Name },
					{ nameof(Repository) + nameof(Repository.OwnerLogin), repository.OwnerLogin },
					{ nameof(GitHubUserService) + nameof(GitHubUserService.Alias), _gitHubUserService.Alias },
					{ nameof(GitHubUserService) + nameof(GitHubUserService.Name), _gitHubUserService.Name },
					{ nameof(GitHubApiStatusService) + nameof(GitHubApiStatusService.IsAbuseRateLimit),  _gitHubApiStatusService.IsAbuseRateLimit(e, out _).ToString() }
				});
			}
		}

		async Task<(string RepositoryName, StarGazers? StarGazers)> GetStarGazersStatistics(Repository repository, CancellationToken cancellationToken)
		{
			try
			{
				var starGazers = await _gitHubGraphQLApiService.GetStarGazers(repository.Name, repository.OwnerLogin, cancellationToken).ConfigureAwait(false);
				return (repository.Name, starGazers);
			}
			catch (ApiException e) when (_gitHubApiStatusService.IsAbuseRateLimit(e.Headers, out var timespan))
			{
				OnAbuseRateLimitFound_UpdateRepositoriesWithViewsClonesAndStarsData(repository, timespan.Value);

				return (repository.Name, null);
			}
			catch (ApiException e) when (e.StatusCode is System.Net.HttpStatusCode.Forbidden)
			{
				reportException(e);

				return (repository.Name, null);
			}
			catch (GraphQLException<StarGazers> e) when (e.ContainsSamlOrganizationAthenticationError(out _))
			{
				reportException(e);

				return (repository.Name, null);
			}
			catch (ApiException e) when (e.StatusCode is System.Net.HttpStatusCode.NotFound) // Repository deleted from GitHub but has not yet been deleted from local SQLite Database
			{
				reportException(e);

				OnRepositoryUriNotFound(repository.Url);

				return (repository.Name, null);
			}

			void reportException(in Exception e)
			{
				_analyticsService.Report(e, new Dictionary<string, string>
				{
					{ nameof(Repository) + nameof(Repository.Name), repository.Name },
					{ nameof(Repository) + nameof(Repository.OwnerLogin), repository.OwnerLogin },
					{ nameof(GitHubUserService) + nameof(GitHubUserService.Alias), _gitHubUserService.Alias },
					{ nameof(GitHubUserService) + nameof(GitHubUserService.Name), _gitHubUserService.Name },
					{ nameof(GitHubApiStatusService) + nameof(GitHubApiStatusService.IsAbuseRateLimit),  _gitHubApiStatusService.IsAbuseRateLimit(e, out _).ToString() }
				});
			}
		}

		void OnAbuseRateLimitFound_UpdateRepositoriesWithViewsClonesAndStarsData(in Repository repository, in TimeSpan retryTimeSpan) =>
			_abuseRateLimitFoundEventManager.RaiseEvent(this, (repository, retryTimeSpan), nameof(AbuseRateLimitFound_UpdateRepositoriesWithViewsClonesAndStarsData));

		void OnAbuseRateLimitFound_GetReferringSites(in Repository repository, in TimeSpan retryTimeSpan) =>
			_abuseRateLimitFoundEventManager.RaiseEvent(this, (repository, retryTimeSpan), nameof(AbuseRateLimitFound_GetReferringSites));

		void OnRepositoryUriNotFound(in string url) =>
			_repositoryUriNotFoundEventManager.RaiseEvent(this, new Uri(url), nameof(RepositoryUriNotFound));
	}
}
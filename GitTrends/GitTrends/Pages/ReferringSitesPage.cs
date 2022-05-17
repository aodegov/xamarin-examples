using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GitTrends.Mobile.Common;
using GitTrends.Mobile.Common.Constants;
using GitTrends.Shared;
using Xamarin.CommunityToolkit.Markup;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using static Xamarin.CommunityToolkit.Markup.GridRowsColumns;

namespace GitTrends
{
	partial class ReferringSitesPage : BaseContentPage<ReferringSitesViewModel>
	{
		const int _titleTopMargin = 10;

		readonly CancellationTokenSource _refreshViewCancelltionTokenSource = new();

		readonly Repository _repository;
		readonly RefreshView _refreshView;
		readonly ThemeService _themeService;
		readonly ReviewService _reviewService;
		readonly GitHubUserService _gitHubUserService;
		readonly DeepLinkingService _deepLinkingService;

		public ReferringSitesPage(IMainThread mainThread,
									Repository repository,
									ThemeService themeService,
									ReviewService reviewService,
									IAnalyticsService analyticsService,
									GitHubUserService gitHubUserService,
									DeepLinkingService deepLinkingService,
									ReferringSitesViewModel referringSitesViewModel) : base(referringSitesViewModel, analyticsService, mainThread)
		{
			Title = PageTitles.ReferringSitesPage;

			_repository = repository;
			_themeService = themeService;
			_reviewService = reviewService;
			_gitHubUserService = gitHubUserService;
			_deepLinkingService = deepLinkingService;

			ReferringSitesViewModel.PullToRefreshFailed += HandlePullToRefreshFailed;

			var isiOS = Device.RuntimePlatform is Device.iOS;
			var titleRowHeight = isiOS ? 50 : 0;
			var shadowHeight = isiOS ? 1 : 0;

			var collectionView = new ReferringSitesCollectionView()
				.Bind(IsVisibleProperty, nameof(ReferringSitesViewModel.IsEmptyDataViewEnabled))
				.Bind(CollectionView.ItemsSourceProperty, nameof(ReferringSitesViewModel.MobileReferringSitesList))
				.Invoke(collectionView => collectionView.SelectionChanged += HandleCollectionViewSelectionChanged);

			Content = new Grid
			{
				RowSpacing = 0,

				RowDefinitions = Rows.Define(
					(Row.Title, Auto),
					(Row.TitleShadow, shadowHeight),
					(Row.List, Star)),

				ColumnDefinitions = Columns.Define(
					(Column.Title, Stars(3)),
					(Column.Button, Stars(1))),

				Children =
				{
					new ReferringSitesRefreshView(collectionView, repository, _refreshViewCancelltionTokenSource.Token).Assign(out _refreshView)
						.Row(Row.TitleShadow).RowSpan(3).ColumnSpan(All<Column>())
						.Bind(RefreshView.CommandProperty, nameof(ReferringSitesViewModel.RefreshCommand))
						.Bind(RefreshView.IsRefreshingProperty, nameof(ReferringSitesViewModel.IsRefreshing))
						.DynamicResource(RefreshView.RefreshColorProperty, nameof(BaseTheme.PullToRefreshColor)),
				}
			};

			if (isiOS)
			{
				var grid = (Grid)Content;

				grid.Children.Add(new TitleShadowView(themeService, titleRowHeight, shadowHeight).Row(Row.Title).ColumnSpan(All<Column>()));
				grid.Children.Add(new TitleLabel().Row(Row.Title).Column(Column.Title));
				grid.Children.Add(new CloseButton(titleRowHeight).Invoke(closeButton => closeButton.Clicked += HandleCloseButtonClicked).Row(Row.Title).Column(Column.Button));
			}
		}

		enum Row { Title, TitleShadow, List }
		enum Column { Title, Button }

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			if (_refreshView?.Content is CollectionView collectionView
				&& collectionView.ItemsSource.IsNullOrEmpty())
			{
				_refreshView.IsRefreshing = true;
				await _reviewService.TryRequestReviewPrompt();
			}
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();

			_refreshViewCancelltionTokenSource.Cancel();
		}

		async void HandleCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var collectionView = (CollectionView)sender;
			collectionView.SelectedItem = null;

			if (e?.CurrentSelection.FirstOrDefault() is ReferringSiteModel referingSite
				&& referingSite.IsReferrerUriValid
				&& referingSite.ReferrerUri != null)
			{
				AnalyticsService.Track("Referring Site Tapped", new Dictionary<string, string>
				{
					{ nameof(ReferringSiteModel) + nameof(ReferringSiteModel.Referrer), referingSite.Referrer },
					{ nameof(ReferringSiteModel) + nameof(ReferringSiteModel.ReferrerUri), referingSite.ReferrerUri.ToString() }
				});

				await _deepLinkingService.OpenBrowser(referingSite.ReferrerUri);
			}
		}

		void HandlePullToRefreshFailed(object sender, PullToRefreshFailedEventArgs eventArgs) => MainThread.BeginInvokeOnMainThread(async () =>
		{
			if (Xamarin.Forms.Application.Current.MainPage.Navigation.ModalStack.LastOrDefault() is ReferringSitesPage
				|| Xamarin.Forms.Application.Current.MainPage.Navigation.NavigationStack.Last() is ReferringSitesPage)
			{
				switch (eventArgs)
				{
					case MaximumApiRequestsReachedEventArgs:
						var isAccepted = await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Accept, eventArgs.Cancel);
						if (isAccepted)
							await _deepLinkingService.OpenBrowser(GitHubConstants.GitHubRateLimitingDocs);
						break;

					case AbuseLimitPullToRefreshEventArgs when _gitHubUserService.GitHubApiAbuseLimitCount <= 1:
						var isAlertAccepted = await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Accept, eventArgs.Cancel);
						if (isAlertAccepted)
							await _deepLinkingService.OpenBrowser(GitHubConstants.GitHubApiAbuseDocs);
						break;

					case AbuseLimitPullToRefreshEventArgs:
						// Don't display error message when GitHubUserService.GitHubApiAbuseLimitCount > 1
						break;

					case LoginExpiredPullToRefreshEventArgs:
						await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Cancel);
						await Navigation.PopToRootAsync();
						break;

					case ErrorPullToRefreshEventArgs:
						await DisplayAlert(eventArgs.Title, eventArgs.Message, eventArgs.Cancel);
						break;

					default:
						throw new NotSupportedException();
				}
			}
		});

		async void HandleCloseButtonClicked(object sender, EventArgs e) => await Navigation.PopModalAsync();

		class ReferringSitesRefreshView : RefreshView
		{
			public ReferringSitesRefreshView(in CollectionView collectionView, in Repository repository, in CancellationToken cancellationToken)
			{
				Content = collectionView;
				AutomationId = ReferringSitesPageAutomationIds.RefreshView;
				CommandParameter = (repository, cancellationToken);
			}
		}

		class ReferringSitesCollectionView : CollectionView
		{
			public ReferringSitesCollectionView()
			{
				AutomationId = ReferringSitesPageAutomationIds.CollectionView;
				BackgroundColor = Color.Transparent;
				ItemTemplate = new ReferringSitesDataTemplateSelector();
				SelectionMode = SelectionMode.Single;
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);

				//iOS Header + Footer break CollectionView after Refresh bug: https://github.com/xamarin/Xamarin.Forms/issues/9879
				Header = Device.RuntimePlatform is Device.iOS ? null : new BoxView { HeightRequest = ReferringSitesDataTemplateSelector.BottomPadding };
				Footer = Device.RuntimePlatform is Device.iOS ? null : new BoxView { HeightRequest = ReferringSitesDataTemplateSelector.TopPadding };
				EmptyView = new EmptyDataView("EmptyReferringSitesList", ReferringSitesPageAutomationIds.EmptyDataView)
								.Bind(EmptyDataView.TitleProperty, nameof(ReferringSitesViewModel.EmptyDataViewTitle))
								.Bind(EmptyDataView.DescriptionProperty, nameof(ReferringSitesViewModel.EmptyDataViewDescription));
			}
		}

		class TitleShadowView : BoxView
		{
			public TitleShadowView(in ThemeService themeService, in double heightRequest, in double shadowHeight)
			{
				HeightRequest = heightRequest;

				this.DynamicResource(BackgroundColorProperty, nameof(BaseTheme.CardSurfaceColor));
				if (isLightTheme(themeService.Preference))
				{
					On<iOS>()
						.SetIsShadowEnabled(true)
						.SetShadowColor(Color.Gray)
						.SetShadowOffset(new Size(0, shadowHeight))
						.SetShadowOpacity(0.5)
						.SetShadowRadius(4);
				}

				static bool isLightTheme(in PreferredTheme preferredTheme) => preferredTheme is PreferredTheme.Light || preferredTheme is PreferredTheme.Default && Xamarin.Forms.Application.Current.RequestedTheme is OSAppTheme.Light;
			}
		}

		class TitleLabel : Label
		{
			public TitleLabel()
			{
				Text = PageTitles.ReferringSitesPage;
				Padding = new Thickness(10, 5, 0, 5);

				this.Font(family: FontFamilyConstants.RobotoMedium, size: 30);
				this.StartExpand().TextCenterVertical().TextStart();
				this.DynamicResource(TextColorProperty, nameof(BaseTheme.TextColor));
			}
		}

		class CloseButton : Button
		{
			public CloseButton(in int titleRowHeight)
			{
				Text = ReferringSitesPageConstants.CloseButtonText;
				AutomationId = ReferringSitesPageAutomationIds.CloseButton;

				this.Font(family: FontFamilyConstants.RobotoRegular);
				this.End().CenterVertical().Margins(right: 10).Height(titleRowHeight * 3 / 5).Padding(5, 0);

				this.DynamicResources((TextColorProperty, nameof(BaseTheme.CloseButtonTextColor)),
										(BackgroundColorProperty, nameof(BaseTheme.CloseButtonBackgroundColor)));
			}
		}
	}
}
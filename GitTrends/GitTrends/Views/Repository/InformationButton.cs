﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using GitTrends.Mobile.Common;
using GitTrends.Shared;
using Xamarin.CommunityToolkit.Markup;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;
using static Xamarin.CommunityToolkit.Markup.GridRowsColumns;

namespace GitTrends
{
	class InformationButton : Grid
	{
		public const int Diameter = 100;

		readonly IMainThread _mainThread;
		readonly IAnalyticsService _analyticsService;
		readonly FloatingActionTextButton _totalButton, _statistic1FloatingActionButton, _statistic2FloatingActionButton, _statistic3FloatingActionButton;

		public InformationButton(MobileSortingService mobileSortingService, in IMainThread mainThread, in IAnalyticsService analyticsService)
		{
			_mainThread = mainThread;
			_analyticsService = analyticsService;

			AutomationId = RepositoryPageAutomationIds.InformationButton;

			RowDefinitions = Rows.Define(Diameter);
			ColumnDefinitions = Columns.Define(Diameter);

			Children.Add(new FloatingActionTextButton(mobileSortingService, FloatingActionButtonType.Statistic1, FloatingActionButtonSize.Mini).Center().Assign(out _statistic1FloatingActionButton)
							.Bind<FloatingActionTextButton, IReadOnlyList<Repository>, string>(FloatingActionTextButton.TextProperty, nameof(RepositoryViewModel.VisibleRepositoryList), BindingMode.OneWay, convert: repositories => repositories is null ? string.Empty : StatisticsService.GetFloatingActionTextButtonText(mobileSortingService, repositories, FloatingActionButtonType.Statistic1)));

			Children.Add(new FloatingActionTextButton(mobileSortingService, FloatingActionButtonType.Statistic2, FloatingActionButtonSize.Mini).Center().Assign(out _statistic2FloatingActionButton)
							.Bind<FloatingActionTextButton, IReadOnlyList<Repository>, string>(FloatingActionTextButton.TextProperty, nameof(RepositoryViewModel.VisibleRepositoryList), BindingMode.OneWay, convert: repositories => repositories is null ? string.Empty : StatisticsService.GetFloatingActionTextButtonText(mobileSortingService, repositories, FloatingActionButtonType.Statistic2)));

			Children.Add(new FloatingActionTextButton(mobileSortingService, FloatingActionButtonType.Statistic3, FloatingActionButtonSize.Mini).Center().Assign(out _statistic3FloatingActionButton)
							.Bind<FloatingActionTextButton, IReadOnlyList<Repository>, string>(FloatingActionTextButton.TextProperty, nameof(RepositoryViewModel.VisibleRepositoryList), BindingMode.OneWay, convert: repositories => repositories is null ? string.Empty : StatisticsService.GetFloatingActionTextButtonText(mobileSortingService, repositories, FloatingActionButtonType.Statistic3)));

			Children.Add(new FloatingActionTextButton(mobileSortingService, FloatingActionButtonType.Information, FloatingActionButtonSize.Normal, new AsyncCommand(ExecuteFloatingActionButtonCommand)) { FontFamily = FontFamilyConstants.RobotoMedium }.Center().Assign(out _totalButton)
							.Bind(FloatingActionTextButton.TextProperty, nameof(RepositoryViewModel.TotalButtonText)));

			SetBinding(IsVisibleProperty, new MultiBinding
			{
				Converter = new InformationMultiValueConverter(),
				Bindings =
				{
					new Binding(nameof(RepositoryViewModel.IsRefreshing), BindingMode.OneWay),
					new Binding(nameof(RepositoryViewModel.VisibleRepositoryList), BindingMode.OneWay)
				}
			});
		}

		Task ExecuteFloatingActionButtonCommand()
		{
			const int animationDuration = 300;
			const string informationButtonTapped = "Information Button Tapped";

			return _mainThread.InvokeOnMainThreadAsync(async () =>
			{
				bounceButton(_totalButton);

				if (isVisible(_statistic1FloatingActionButton) && isVisible(_statistic2FloatingActionButton) && isVisible(_statistic3FloatingActionButton))
				{
					_analyticsService.Track(informationButtonTapped, nameof(isVisible), "true");

					await Task.WhenAll(_statistic1FloatingActionButton.TranslateTo(0, 0, animationDuration, Easing.SpringIn), _statistic1FloatingActionButton.RotateTo(0, animationDuration, Easing.SpringIn),
										   _statistic2FloatingActionButton.TranslateTo(0, 0, animationDuration, Easing.SpringIn), _statistic2FloatingActionButton.RotateTo(0, animationDuration, Easing.SpringIn),
										   _statistic3FloatingActionButton.TranslateTo(0, 0, animationDuration, Easing.SpringIn), _statistic3FloatingActionButton.RotateTo(0, animationDuration, Easing.SpringIn));
				}
				else
				{
					_analyticsService.Track(informationButtonTapped, nameof(isVisible), "false");

					await Task.WhenAll(_statistic1FloatingActionButton.TranslateTo(-75, 10, animationDuration, Easing.SpringOut), _statistic1FloatingActionButton.RotateTo(360, animationDuration, Easing.SpringOut),
											_statistic2FloatingActionButton.TranslateTo(-50, -50, animationDuration, Easing.SpringOut), _statistic2FloatingActionButton.RotateTo(360, animationDuration, Easing.SpringOut),
											_statistic3FloatingActionButton.TranslateTo(10, -75, animationDuration, Easing.SpringOut), _statistic3FloatingActionButton.RotateTo(360, animationDuration, Easing.SpringOut));
				}
			});

			static bool isVisible(in FloatingActionTextButton statisticButton) => statisticButton.TranslationX != 0 && statisticButton.TranslationY != 0;
			static async void bounceButton(FloatingActionTextButton floatingActionTextButton)
			{
				await floatingActionTextButton.ScaleTo(1.1, animationDuration / 2);
				await floatingActionTextButton.ScaleTo(1, animationDuration / 2);
			}
		}

		class InformationMultiValueConverter : IMultiValueConverter
		{
			public object? Convert(object?[]? values, Type? targetType, object? parameter, CultureInfo? culture)
			{
				if (values?[0] is bool isRefreshing && values[1] is IReadOnlyList<Repository> repositoryList)
					return !isRefreshing && repositoryList.Any();

				return false;
			}

			public object?[]? ConvertBack(object? value, Type?[]? targetTypes, object? parameter, CultureInfo? culture) => throw new NotImplementedException();
		}

		class FloatingActionTextButton : FloatingActionButtonView
		{
			public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(FloatingActionTextButton), string.Empty);
			public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(FloatingActionTextButton), null);

			readonly MobileSortingService _mobileSortingService;
			readonly FloatingActionButtonType _floatingActionButtonType;

			public FloatingActionTextButton(in MobileSortingService mobileSortingService,
											FloatingActionButtonType floatingActionButtonType,
											in FloatingActionButtonSize floatingActionButtonSize,
											in ICommand? command = null)
			{
				_mobileSortingService = mobileSortingService;
				_floatingActionButtonType = floatingActionButtonType;

				ThemeService.PreferenceChanged += HandlePreferenceChanged;

				Size = floatingActionButtonSize;

				Command = command;

				Content = new TextLabel(floatingActionButtonType).Center().TextCenter()
								.Bind(Label.TextProperty, nameof(Text), source: this)
								.Bind(Label.FontFamilyProperty, nameof(FontFamily), source: this)
								.Bind<Label, string, double>(Label.FontSizeProperty, nameof(Text), source: this, convert: text => convertFontSize(text, floatingActionButtonType));


				this.Bind<FloatingActionButtonView, IReadOnlyList<Repository>, Color>(FloatingActionButtonBackgroundColorProperty, nameof(RepositoryViewModel.VisibleRepositoryList), convert: repositories => GetBackgroundColor());

				static double convertFontSize(in string? text, in FloatingActionButtonType floatingActionButtonType) => (floatingActionButtonType, text?.Length) switch
				{
					(_, null) => (double)Label.FontSizeProperty.DefaultValue,
					(FloatingActionButtonType.Statistic1, _) => 10,
					(FloatingActionButtonType.Statistic2, _) => 10,
					(FloatingActionButtonType.Statistic3, _) => 10,
					(FloatingActionButtonType.Information, <= 5) => 13,
					(FloatingActionButtonType.Information, <= 8) => 10,
					(FloatingActionButtonType.Information, > 8) => 7.5,
					(_, _) => throw new NotImplementedException()
				};
			}

			public string Text
			{
				get => (string)GetValue(TextProperty);
				set => SetValue(TextProperty, value);
			}

			public string? FontFamily
			{
				get => (string?)GetValue(FontFamilyProperty);
				set => SetValue(FontFamilyProperty, value);
			}

			Color GetBackgroundColor()
			{
				var color = (MobileSortingService.GetSortingCategory(_mobileSortingService.CurrentOption), _floatingActionButtonType) switch
				{
					(SortingCategory.Clones, FloatingActionButtonType.Statistic1) => (Color)Application.Current.Resources[nameof(BaseTheme.CardClonesStatsIconColor)],
					(SortingCategory.Clones, FloatingActionButtonType.Statistic2) => (Color)Application.Current.Resources[nameof(BaseTheme.CardUniqueClonesStatsIconColor)],
					(SortingCategory.Clones, FloatingActionButtonType.Statistic3) => (Color)Application.Current.Resources[nameof(BaseTheme.CardStarsStatsIconColor)],
					(SortingCategory.Views, FloatingActionButtonType.Statistic1) => (Color)Application.Current.Resources[nameof(BaseTheme.CardViewsStatsIconColor)],
					(SortingCategory.Views, FloatingActionButtonType.Statistic2) => (Color)Application.Current.Resources[nameof(BaseTheme.CardUniqueViewsStatsIconColor)],
					(SortingCategory.Views, FloatingActionButtonType.Statistic3) => (Color)Application.Current.Resources[nameof(BaseTheme.CardStarsStatsIconColor)],
					(SortingCategory.IssuesForks, FloatingActionButtonType.Statistic1) => (Color)Application.Current.Resources[nameof(BaseTheme.CardStarsStatsIconColor)],
					(SortingCategory.IssuesForks, FloatingActionButtonType.Statistic2) => (Color)Application.Current.Resources[nameof(BaseTheme.CardForksStatsIconColor)],
					(SortingCategory.IssuesForks, FloatingActionButtonType.Statistic3) => (Color)Application.Current.Resources[nameof(BaseTheme.CardIssuesStatsIconColor)],
					(_, FloatingActionButtonType.Information) => Color.FromHex(BaseTheme.LightTealColorHex).AddLuminosity(.05),
					(_, _) => throw new NotImplementedException()
				};

				return color.AddLuminosity(.1);
			}

			void HandlePreferenceChanged(object sender, PreferredTheme e) => FloatingActionButtonBackgroundColor = GetBackgroundColor();

			class TextLabel : Label
			{
				public TextLabel(in FloatingActionButtonType floatingActionButtonType)
				{
					TextColor = Color.Black;
					TextTransform = TextTransform.Uppercase;

					FontFamily = FontFamilyConstants.RobotoBold;

					InputTransparent = true;

					LineBreakMode = LineBreakMode.TailTruncation;

					Padding = new Thickness(4, 0);

					AutomationId = RepositoryPageAutomationIds.GetFloatingActionTextButtonLabelAutomationId(floatingActionButtonType);
				}
			}
		}
	}
}
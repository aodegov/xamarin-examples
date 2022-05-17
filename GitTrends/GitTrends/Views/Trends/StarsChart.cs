﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GitTrends.Mobile.Common;
using GitTrends.Mobile.Common.Constants;
using Syncfusion.SfChart.XForms;
using Xamarin.CommunityToolkit.Markup;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace GitTrends
{
	class StarsChart : BaseChartView
	{
		public StarsChart(IMainThread mainThread) : base(new StarsTrendsChart(mainThread))
		{
		}

		class StarsTrendsChart : BaseTrendsChart
		{
			//MinimumStarCount > MaximumDays > MaximumStarCount
			const int _maximumDays = 365;
			const int _minimumStarCount = 10;
			const int _maximumStarCount = 100;

			public StarsTrendsChart(IMainThread mainThread) : base(mainThread, TrendsPageAutomationIds.StarsChart)
			{
				var primaryAxisLabelStyle = new ChartAxisLabelStyle
				{
					FontSize = 9,
					FontFamily = FontFamilyConstants.RobotoRegular,
					Margin = new Thickness(2, 4, 2, 0)
				}.DynamicResource(ChartLabelStyle.TextColorProperty, nameof(BaseTheme.ChartAxisTextColor));

				var axisLineStyle = new ChartLineStyle()
				{
					StrokeWidth = 1.51
				}.DynamicResource(ChartLineStyle.StrokeColorProperty, nameof(BaseTheme.ChartAxisLineColor));

				PrimaryAxis = new DateTimeAxis
				{
					IntervalType = DateTimeIntervalType.Months,
					Interval = 1,
					RangePadding = DateTimeRangePadding.Round,
					LabelStyle = primaryAxisLabelStyle,
					AxisLineStyle = axisLineStyle,
					MajorTickStyle = new ChartAxisTickStyle { StrokeColor = Color.Transparent },
					ShowMajorGridLines = false,
				};
				PrimaryAxis.SetBinding(DateTimeAxis.MinimumProperty, nameof(TrendsViewModel.MinDailyStarsDate));
				PrimaryAxis.SetBinding(DateTimeAxis.MaximumProperty, nameof(TrendsViewModel.MaxDailyStarsDate));

				var secondaryAxisMajorTickStyle = new ChartAxisTickStyle().DynamicResource(ChartAxisTickStyle.StrokeColorProperty, nameof(BaseTheme.ChartAxisLineColor));

				var secondaryAxisLabelStyle = new ChartAxisLabelStyle
				{
					FontSize = 12,
					FontFamily = FontFamilyConstants.RobotoRegular,
				}.DynamicResource(ChartLabelStyle.TextColorProperty, nameof(BaseTheme.ChartAxisTextColor));

				SecondaryAxis = new NumericalAxis
				{
					LabelStyle = secondaryAxisLabelStyle,
					AxisLineStyle = axisLineStyle,
					MajorTickStyle = secondaryAxisMajorTickStyle,
					ShowMajorGridLines = false,
				}.Bind(NumericalAxis.MinimumProperty, nameof(TrendsViewModel.MinDailyStarsValue))
				 .Bind(NumericalAxis.MaximumProperty, nameof(TrendsViewModel.MaxDailyStarsValue))
				 .Bind(NumericalAxis.IntervalProperty, nameof(TrendsViewModel.StarsChartYAxisInterval));

				StarsSeries = new TrendsAreaSeries(TrendsChartTitleConstants.StarsTitle, nameof(DailyStarsModel.LocalDay), nameof(DailyStarsModel.TotalStars), nameof(BaseTheme.CardStarsStatsIconColor));
				StarsSeries.SetBinding(ChartSeries.ItemsSourceProperty, nameof(TrendsViewModel.DailyStarsList));
				StarsSeries.PropertyChanged += HandleStarSeriesPropertyChanged;

				Series = new ChartSeriesCollection { StarsSeries };

				this.SetBinding(IsVisibleProperty, nameof(TrendsViewModel.IsStarsChartVisible));
			}

			public AreaSeries StarsSeries { get; }

			async Task ZoomStarsChart(IReadOnlyList<DailyStarsModel> dailyStarsList)
			{
				if (dailyStarsList.Any())
				{
					var mostRecentDailyStarsModel = dailyStarsList[^1];

					var maximumDaysDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(_maximumDays));

					//Zoom to Maximum Stars
					if (dailyStarsList.Count >= _maximumStarCount)
					{
						var maximumStarsDailyStarsModel = dailyStarsList[^_maximumStarCount];

						await SetZoom(maximumStarsDailyStarsModel.LocalDay.ToOADate(),
										mostRecentDailyStarsModel.LocalDay.ToOADate(),
										maximumStarsDailyStarsModel.TotalStars,
										mostRecentDailyStarsModel.TotalStars);
					}
					//Zoom to Maximum Days when Minimum Star Count has been met
					else if (dailyStarsList[0].Day <= maximumDaysDateTime)
					{
						var nearestDailyStarsModel = getNearestDailyStarsModelToTimeStamp(dailyStarsList, maximumDaysDateTime);

						if (mostRecentDailyStarsModel.TotalStars - nearestDailyStarsModel.TotalStars > _minimumStarCount)
						{

							await SetZoom(maximumDaysDateTime.LocalDateTime.ToOADate(),
											mostRecentDailyStarsModel.LocalDay.ToOADate(),
											nearestDailyStarsModel.TotalStars,
											mostRecentDailyStarsModel.TotalStars);
						}
					}
				}

				//https://stackoverflow.com/a/1757221/5953643
				static DailyStarsModel getNearestDailyStarsModelToTimeStamp(in IReadOnlyList<DailyStarsModel> dailyStarsList, DateTimeOffset timeStamp)
				{
					var starsListOrderedByProximityToTimeStamp = dailyStarsList.OrderBy(t => Math.Abs((t.Day - timeStamp).Ticks));

					foreach (var dailyStarsModel in starsListOrderedByProximityToTimeStamp)
					{
						//Get the nearest DailyStarsModel before timeStamp
						if (dailyStarsModel.Day < timeStamp)
							return dailyStarsModel;
					}

					return starsListOrderedByProximityToTimeStamp.First();
				}
			}

			void HandleStarSeriesPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName is nameof(ChartSeries.ItemsSource))
				{
					var trendsAreaSeries = (TrendsAreaSeries)sender;
					var dailyStarsList = (IReadOnlyList<DailyStarsModel>)trendsAreaSeries.ItemsSource;

					if (dailyStarsList.Any())
					{
						//Wait for SFChart to finish Rendering before Zooming
						PropertyChanged += HandleSFChartPropertyChanged;
					}

					async void HandleSFChartPropertyChanged(object sender, PropertyChangedEventArgs e)
					{
						if (e.PropertyName is "Renderer")
						{
							PropertyChanged -= HandleSFChartPropertyChanged;

							//Yeild to the UI thread to allow the render to finish
							await Task.Yield();

							await ZoomStarsChart(dailyStarsList);
						}
					}
				}
			}
		}
	}
}
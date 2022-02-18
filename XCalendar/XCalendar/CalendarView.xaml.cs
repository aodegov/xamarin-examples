﻿using XCalendar.Extensions;
using XCalendar.Enums;
using XCalendar.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XCalendar
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarView : ContentView
    {
        #region Fields
        protected static readonly List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>()
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday,
        };

        private readonly ObservableCollection<CalendarDay> _Days = new ObservableCollection<CalendarDay>();
        private readonly ObservableRangeCollection<DayOfWeek> _StartOfWeekDayNamesOrder = new ObservableRangeCollection<DayOfWeek>();
        private readonly List<DateTime> _PreviousSelectedDates = new List<DateTime>();
        #endregion

        #region Properties

        #region Bindable Properties
        /// <summary>
        /// The list of displayed days.
        /// </summary>
        public ReadOnlyObservableCollection<CalendarDay> Days
        {
            get { return (ReadOnlyObservableCollection<CalendarDay>)GetValue(DaysProperty); }
            protected set { SetValue(DaysPropertyKey, value); }
        }
        /// <summary>
        /// The date the calendar will use to get the dates representing a time unit.
        /// </summary>
        public DateTime NavigatedDate
        {
            get { return (DateTime)GetValue(NavigatedDateProperty); }
            set { SetValue(NavigatedDateProperty, value); }
        }
        /// <summary>
        /// The date that the calendar should consider as 'Today'.
        /// </summary>
        public DateTime TodayDate
        {
            get { return (DateTime)GetValue(TodayDateProperty); }
            set { SetValue(TodayDateProperty, value); }
        }
        /// <summary>
        /// The lower bound of the day range.
        /// </summary>
        /// <seealso cref="NavigationLoopMode"/>
        public DateTime DayRangeMinimumDate
        {
            get { return (DateTime)GetValue(DayRangeMinimumDateProperty); }
            set { SetValue(DayRangeMinimumDateProperty, value); }
        }
        /// <summary>
        /// The upper bound of the day range.
        /// </summary>
        /// <seealso cref="NavigationLoopMode"/>
        public DateTime DayRangeMaximumDate
        {
            get { return (DateTime)GetValue(DayRangeMaximumDateProperty); }
            set { SetValue(DayRangeMaximumDateProperty, value); }
        }
        /// <summary>
        /// The day of week that should be considered as the start of the week.
        /// </summary>
        /// <seealso cref="CustomDayNamesOrder"/>
        public DayOfWeek StartOfWeek
        {
            get { return (DayOfWeek)GetValue(StartOfWeekProperty); }
            set { SetValue(StartOfWeekProperty, value); }
        }
        /// <summary>
        /// The individual height of the displayed <see cref="Days"/>.
        /// </summary>
        public double DayHeightRequest
        {
            get { return (double)GetValue(DayHeightRequestProperty); }
            set { SetValue(DayHeightRequestProperty, value); }
        }
        public double DayWidthRequest
        {
            get { return (double)GetValue(DayWidthRequestProperty); }
            set { SetValue(DayWidthRequestProperty, value); }
        }
        public Color DayCurrentMonthTextColor
        {
            get { return (Color)GetValue(DayCurrentMonthTextColorProperty); }
            set { SetValue(DayCurrentMonthTextColorProperty, value); }
        }
        public Color DayCurrentMonthBackgroundColor
        {
            get { return (Color)GetValue(DayCurrentMonthBackgroundColorProperty); }
            set { SetValue(DayCurrentMonthBackgroundColorProperty, value); }
        }
        public Color DayCurrentMonthBorderColor
        {
            get { return (Color)GetValue(DayCurrentMonthBorderColorProperty); }
            set { SetValue(DayCurrentMonthBorderColorProperty, value); }
        }
        public Color DayTodayTextColor
        {
            get { return (Color)GetValue(DayTodayTextColorProperty); }
            set { SetValue(DayTodayTextColorProperty, value); }
        }
        public Color DayTodayBackgroundColor
        {
            get { return (Color)GetValue(DayTodayBackgroundColorProperty); }
            set { SetValue(DayTodayBackgroundColorProperty, value); }
        }
        public Color DayTodayBorderColor
        {
            get { return (Color)GetValue(DayTodayBorderColorProperty); }
            set { SetValue(DayTodayBorderColorProperty, value); }
        }
        public Color DayOtherMonthTextColor
        {
            get { return (Color)GetValue(DayOtherMonthTextColorProperty); }
            set { SetValue(DayOtherMonthTextColorProperty, value); }
        }
        public Color DayOtherMonthBackgroundColor
        {
            get { return (Color)GetValue(DayOtherMonthBackgroundColorProperty); }
            set { SetValue(DayOtherMonthBackgroundColorProperty, value); }
        }
        public Color DayOtherMonthBorderColor
        {
            get { return (Color)GetValue(DayOtherMonthBorderColorProperty); }
            set { SetValue(DayOtherMonthBorderColorProperty, value); }
        }
        public Color DayOutOfRangeTextColor
        {
            get { return (Color)GetValue(DayOutOfRangeTextColorProperty); }
            set { SetValue(DayOutOfRangeTextColorProperty, value); }
        }
        public Color DayOutOfRangeBackgroundColor
        {
            get { return (Color)GetValue(DayOutOfRangeBackgroundColorProperty); }
            set { SetValue(DayOutOfRangeBackgroundColorProperty, value); }
        }
        public Color DayOutOfRangeBorderColor
        {
            get { return (Color)GetValue(DayOutOfRangeBorderColorProperty); }
            set { SetValue(DayOutOfRangeBorderColorProperty, value); }
        }
        public Color DaySelectedTextColor
        {
            get { return (Color)GetValue(DaySelectedTextColorProperty); }
            set { SetValue(DaySelectedTextColorProperty, value); }
        }
        public Color DaySelectedBackgroundColor
        {
            get { return (Color)GetValue(DaySelectedBackgroundColorProperty); }
            set { SetValue(DaySelectedBackgroundColorProperty, value); }
        }
        public Color DaySelectedBorderColor
        {
            get { return (Color)GetValue(DaySelectedBorderColorProperty); }
            set { SetValue(DaySelectedBorderColorProperty, value); }
        }
        public ControlTemplate DayNamesTemplate
        {
            get { return (ControlTemplate)GetValue(DayNamesTemplateProperty); }
            set { SetValue(DayNamesTemplateProperty, value); }
        }
        /// <summary>
        /// The height of the view showing the days of the week.
        /// </summary>
        public double DayNamesHeightRequest
        {
            get { return (double)GetValue(DayNamesHeightRequestProperty); }
            set { SetValue(DayNamesHeightRequestProperty, value); }
        }
        /// <summary>
        /// The template used to display the days of the week.
        /// </summary>
        public DataTemplate DayNameTemplate
        {
            get { return (DataTemplate)GetValue(DayNameTemplateProperty); }
            set { SetValue(DayNameTemplateProperty, value); }
        }
        public double DayNameVerticalSpacing
        {
            get { return (double)GetValue(DayNameVerticalSpacingProperty); }
            set { SetValue(DayNameVerticalSpacingProperty, value); }
        }
        public double DayNameHorizontalSpacing
        {
            get { return (double)GetValue(DayNameHorizontalSpacingProperty); }
            set { SetValue(DayNameHorizontalSpacingProperty, value); }
        }
        public Color DayNameTextColor
        {
            get { return (Color)GetValue(DayNameTextColorProperty); }
            set { SetValue(DayNameTextColorProperty, value); }
        }
        /// <summary>
        /// The template used to display the <see cref="Days"/>.
        /// </summary>
        public ControlTemplate MonthViewTemplate
        {
            get { return (ControlTemplate)GetValue(MonthViewTemplateProperty); }
            set { SetValue(MonthViewTemplateProperty, value); }
        }
        /// <summary>
        /// The height of the view used to display the <see cref="Days"/>
        /// </summary>
        public double MonthViewHeightRequest
        {
            get { return (double)GetValue(MonthViewHeightRequestProperty); }
            set { SetValue(MonthViewHeightRequestProperty, value); }
        }
        /// <summary>
        /// Whether to automatically add as many rows as needed to represent the time unit or not.
        /// </summary>
        /// <seealso cref="AutoRowsIsConsistent"/>
        public bool AutoRows
        {
            get { return (bool)GetValue(AutoRowsProperty); }
            set { SetValue(AutoRowsProperty, value); }
        }
        /// <summary>
        /// Whether to make sure the amount of rows stays the same across the time unit.
        /// </summary>
        /// <example>If the <see cref="StartOfWeek"/> is Monday, the highest number of rows needed to display a month in 2022 is 6 rows (May, October etc).
        /// If this property is true, the calendar will display 6 rows regardless of whether a month needs less or not (April, November etc).
        /// Otherwise it will display as needed: (5 for April and November, 6 for May and October etc).</example>
        /// <seealso cref="AutoRows"/>
        public bool AutoRowsIsConsistent
        {
            get { return (bool)GetValue(AutoRowsIsConsistentProperty); }
            set { SetValue(AutoRowsIsConsistentProperty, value); }
        }
        /// <summary>
        /// Whether to use <see cref="CustomDayNamesOrder"/> for displaying the page or not.
        /// </summary>
        public bool UseCustomDayNamesOrder
        {
            get { return (bool)GetValue(UseCustomDayNamesOrderProperty); }
            set { SetValue(UseCustomDayNamesOrderProperty, value); }
        }
        /// <summary>
        /// The template used to display the view for navigating the calendar.
        /// </summary>
        public ControlTemplate NavigationTemplate
        {
            get { return (ControlTemplate)GetValue(NavigationTemplateProperty); }
            set { SetValue(NavigationTemplateProperty, value); }
        }
        public Color NavigationTextColor
        {
            get { return (Color)GetValue(NavigationTextColorProperty); }
            set { SetValue(NavigationTextColorProperty, value); }
        }
        public Color NavigationArrowColor
        {
            get { return (Color)GetValue(NavigationArrowColorProperty); }
            set { SetValue(NavigationArrowColorProperty, value); }
        }
        public Color NavigationArrowBackgroundColor
        {
            get { return (Color)GetValue(NavigationArrowBackgroundColorProperty); }
            set { SetValue(NavigationArrowBackgroundColorProperty, value); }
        }
        public float NavigationArrowCornerRadius
        {
            get { return (float)GetValue(NavigationArrowCornerRadiusProperty); }
            set { SetValue(NavigationArrowCornerRadiusProperty, value); }
        }
        /// <summary>
        /// The type of selection to use for selecting dates.
        /// </summary>
        public Enums.SelectionMode SelectionMode
        {
            get { return (Enums.SelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }
        /// <summary>
        /// How the calendar handles navigation past the <see cref="DateTime.MinValue"/>, <see cref="DateTime.MaxValue"/>, <see cref="DayRangeMinimumDate"/>, and <see cref="DayRangeMaximumDate"/>.
        /// </summary>
        /// <seealso cref="NavigateCalendar(int)"/>
        public NavigationLoopMode NavigationLoopMode
        {
            get { return (NavigationLoopMode)GetValue(NavigationLoopModeProperty); }
            set { SetValue(NavigationLoopModeProperty, value); }
        }
        /// <summary>
        /// The date that is currently selected. Empty when the <see cref="Enums.SelectionMode"/> is not set to <see cref="Enums.SelectionMode.Multiple"/>.
        /// </summary>
        public ObservableRangeCollection<DateTime> SelectedDates
        {
            get { return (ObservableRangeCollection<DateTime>)GetValue(SelectedDatesProperty); }
            set { SetValue(SelectedDatesProperty, value); }
        }
        /// <summary>
        /// The order to display the days of the week in when <see cref="UseCustomDayNamesOrder"/> is set to true.
        /// </summary>
        /// <remarks>Does not affect logic.</remarks>
        /// <seealso cref="UseCustomDayNamesOrder"/>
        public ObservableRangeCollection<DayOfWeek> CustomDayNamesOrder
        {
            get { return (ObservableRangeCollection<DayOfWeek>)GetValue(CustomDayNamesOrderProperty); }
            set { SetValue(CustomDayNamesOrderProperty, value); }
        }
        /// <summary>
        /// The number of rows to display.
        /// </summary>
        /// <seealso cref="AutoRows"/>
        public int Rows
        {
            get { return (int)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }
        /// <summary>
        /// The template used to display a <see cref="CalendarDay"/>
        /// </summary>
        public DataTemplate DayTemplate
        {
            get { return (DataTemplate)GetValue(DayTemplateProperty); }
            set { SetValue(DayTemplateProperty, value); }
        }
        /// <summary>
        /// The amount that the source date will change when navigating using <see cref="NavigateCalendar(int)"/>.
        /// </summary>
        public NavigationTimeUnit NavigationTimeUnit
        {
            get { return (NavigationTimeUnit)GetValue(NavigationTimeUnitProperty); }
            set { SetValue(NavigationTimeUnitProperty, value); }
        }
        /// <summary>
        /// The way in which to extract a date from the <see cref="NavigatedDate"/> to use as the first date of the first row.
        /// </summary>
        /// <example>If the <see cref="StartOfWeek"/> is Monday and the navigated date is 15th July 2022:
        /// <see cref="PageStartMode.FirstDayOfWeek"/> will extract 11th July 2022.
        /// <see cref="PageStartMode.FirstDayOfMonth"/> will extract 27th June 2022 (First day in the week of 1st July 2022).
        /// <see cref="PageStartMode.FirstDayOfYear"/> will extract 27th December 2021 (First day in the week of 1st January 2022).</example>
        public PageStartMode PageStartMode
        {
            get { return (PageStartMode)GetValue(PageStartModeProperty); }
            set { SetValue(PageStartModeProperty, value); }
        }
        /// <summary>
        /// The days of the week ordered chronologically according to the <see cref="StartOfWeek"/>.
        /// </summary>
        public ReadOnlyObservableCollection<DayOfWeek> StartOfWeekDayNamesOrder
        {
            get { return (ReadOnlyObservableCollection<DayOfWeek>)GetValue(StartOfWeekDayNamesOrderProperty); }
            protected set { SetValue(StartOfWeekDayNamesOrderPropertyKey, value); }
        }
        /// <summary>
        /// The order to display the days of the week in.
        /// </summary>
        public ReadOnlyObservableCollection<DayOfWeek> DayNamesOrder
        {
            get { return (ReadOnlyObservableCollection<DayOfWeek>)GetValue(DayNamesOrderProperty); }
            protected set { SetValue(DayNamesOrderPropertyKey, value); }
        }
        public bool ClampNavigationToDayRange
        {
            get { return (bool)GetValue(ClampNavigationToDayRangeProperty); }
            set { SetValue(ClampNavigationToDayRangeProperty, value); }
        }
        /// <summary>
        /// The height of the view used to display the navigated date and navigation controls.
        /// </summary>
        public double NavigationHeightRequest
        {
            get { return (double)GetValue(NavigationHeightRequestProperty); }
            set { SetValue(NavigationHeightRequestProperty, value); }
        }
        public LayoutOptions DayVerticalOptions
        {
            get { return (LayoutOptions)GetValue(DayVerticalOptionsProperty); }
            set { SetValue(DayVerticalOptionsProperty, value); }
        }
        public LayoutOptions DayHorizontalOptions
        {
            get { return (LayoutOptions)GetValue(DayHorizontalOptionsProperty); }
            set { SetValue(DayHorizontalOptionsProperty, value); }
        }
        public Thickness DayMargin
        {
            get { return (Thickness)GetValue(DayMarginProperty); }
            set { SetValue(DayMarginProperty, value); }
        }
        public Thickness DayPadding
        {
            get { return (Thickness)GetValue(DayPaddingProperty); }
            set { SetValue(DayPaddingProperty, value); }
        }
        public bool DayHasShadow
        {
            get { return (bool)GetValue(DayHasShadowProperty); }
            set { SetValue(DayHasShadowProperty, value); }
        }
        public float DayCornerRadius
        {
            get { return (float)GetValue(DayCornerRadiusProperty); }
            set { SetValue(DayCornerRadiusProperty, value); }
        }
        public int ForwardsNavigationAmount
        {
            get { return (int)GetValue(ForwardsNavigationAmountProperty); }
            set { SetValue(ForwardsNavigationAmountProperty, value); }
        }
        public int BackwardsNavigationAmount
        {
            get { return (int)GetValue(BackwardsNavigationAmountProperty); }
            set { SetValue(BackwardsNavigationAmountProperty, value); }
        }
        public DateTime? RangeSelectionStart
        {
            get { return (DateTime?)GetValue(RangeSelectionStartProperty); }
            set { SetValue(RangeSelectionStartProperty, value); }
        }
        public DateTime? RangeSelectionEnd
        {
            get { return (DateTime?)GetValue(RangeSelectionEndProperty); }
            set { SetValue(RangeSelectionEndProperty, value); }
        }
        public SelectionType SelectionType
        {
            get { return (SelectionType)GetValue(SelectionTypeProperty); }
            set { SetValue(SelectionTypeProperty, value); }
        }
        public Color NavigationBackgroundColor
        {
            get { return (Color)GetValue(NavigationBackgroundColorProperty); }
            set { SetValue(NavigationBackgroundColorProperty, value); }
        }

        #region Bindable Properties Initialisers
        public static readonly BindableProperty NavigatedDateProperty = BindableProperty.Create(nameof(NavigatedDate), typeof(DateTime), typeof(CalendarView), DateTime.Now, defaultBindingMode: BindingMode.TwoWay, propertyChanged: NavigatedDatePropertyChanged, coerceValue: CoerceNavigatedDate);
        private static readonly BindablePropertyKey DaysPropertyKey = BindableProperty.CreateReadOnly(nameof(Days), typeof(ReadOnlyObservableCollection<CalendarDay>), typeof(CalendarView), null, defaultValueCreator: DaysDefaultValueCreator);
        public static readonly BindableProperty DaysProperty = DaysPropertyKey.BindableProperty;
        public static readonly BindableProperty RowsProperty = BindableProperty.Create(nameof(Rows), typeof(int), typeof(CalendarView), 6, defaultBindingMode: BindingMode.TwoWay, propertyChanged: RowsPropertyChanged, validateValue: IsRowsValidValue);
        public static readonly BindableProperty AutoRowsProperty = BindableProperty.Create(nameof(AutoRows), typeof(bool), typeof(CalendarView), true, propertyChanged: AutoRowsPropertyChanged);
        public static readonly BindableProperty AutoRowsIsConsistentProperty = BindableProperty.Create(nameof(AutoRowsIsConsistent), typeof(bool), typeof(CalendarView), true, propertyChanged: AutoRowsIsConsistentPropertyChanged);
        public static readonly BindableProperty DayRangeMinimumDateProperty = BindableProperty.Create(nameof(DayRangeMinimumDate), typeof(DateTime), typeof(CalendarView), DateTime.MinValue, propertyChanged: DayRangeMinimumDatePropertyChanged);
        public static readonly BindableProperty DayRangeMaximumDateProperty = BindableProperty.Create(nameof(DayRangeMaximumDate), typeof(DateTime), typeof(CalendarView), DateTime.MaxValue, propertyChanged: DayRangeMaximumDatePropertyChanged);
        public static readonly BindableProperty TodayDateProperty = BindableProperty.Create(nameof(TodayDate), typeof(DateTime), typeof(CalendarView), DateTime.Today, propertyChanged: TodayDatePropertyChanged);
        public static readonly BindableProperty StartOfWeekProperty = BindableProperty.Create(nameof(StartOfWeek), typeof(DayOfWeek), typeof(CalendarView), CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek, propertyChanged: StartOfWeekPropertyChanged);
        public static readonly BindableProperty SelectionTypeProperty = BindableProperty.Create(nameof(SelectionType), typeof(SelectionType), typeof(CalendarView), SelectionType.None);
        public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(nameof(SelectionMode), typeof(Enums.SelectionMode), typeof(CalendarView), Enums.SelectionMode.Modify);
        public static readonly BindableProperty SelectedDatesProperty = BindableProperty.Create(nameof(SelectedDates), typeof(ObservableRangeCollection<DateTime>), typeof(CalendarView), propertyChanged: SelectedDatesPropertyChanged, defaultValueCreator: SelectedDatesDefaultValueCreator, validateValue: IsSelectedDatesValidValue);
        public static readonly BindableProperty RangeSelectionStartProperty = BindableProperty.Create(nameof(RangeSelectionStart), typeof(DateTime?), typeof(CalendarView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: RangeSelectionStartPropertyChanged);
        public static readonly BindableProperty RangeSelectionEndProperty = BindableProperty.Create(nameof(RangeSelectionEnd), typeof(DateTime?), typeof(CalendarView), defaultBindingMode: BindingMode.TwoWay, propertyChanged: RangeSelectionEndPropertyChanged);
        public static readonly BindableProperty DayTemplateProperty = BindableProperty.Create(nameof(DayTemplate), typeof(DataTemplate), typeof(CalendarView));
        public static readonly BindableProperty DayHeightRequestProperty = BindableProperty.Create(nameof(DayHeightRequest), typeof(double), typeof(CalendarView), 45d);
        public static readonly BindableProperty DayWidthRequestProperty = BindableProperty.Create(nameof(DayWidthRequest), typeof(double), typeof(CalendarView), 45d);
        public static readonly BindableProperty DayVerticalOptionsProperty = BindableProperty.Create(nameof(DayVerticalOptions), typeof(LayoutOptions), typeof(CalendarView), LayoutOptions.Center);
        public static readonly BindableProperty DayHorizontalOptionsProperty = BindableProperty.Create(nameof(DayHorizontalOptions), typeof(LayoutOptions), typeof(CalendarView), LayoutOptions.Center);
        public static readonly BindableProperty DayMarginProperty = BindableProperty.Create(nameof(DayMargin), typeof(Thickness), typeof(CalendarView), new Thickness(2.5));
        public static readonly BindableProperty DayPaddingProperty = BindableProperty.Create(nameof(DayPadding), typeof(Thickness), typeof(CalendarView), new Thickness(0));
        public static readonly BindableProperty DayHasShadowProperty = BindableProperty.Create(nameof(DayHasShadow), typeof(bool), typeof(CalendarView), false);
        public static readonly BindableProperty DayCornerRadiusProperty = BindableProperty.Create(nameof(DayCornerRadius), typeof(float), typeof(CalendarView), 100f);
        public static readonly BindableProperty DayCurrentMonthTextColorProperty = BindableProperty.Create(nameof(DayCurrentMonthTextColor), typeof(Color), typeof(CalendarView), Color.Black);
        public static readonly BindableProperty DayCurrentMonthBackgroundColorProperty = BindableProperty.Create(nameof(DayCurrentMonthBackgroundColor), typeof(Color), typeof(CalendarView), Color.Transparent);
        public static readonly BindableProperty DayCurrentMonthBorderColorProperty = BindableProperty.Create(nameof(DayCurrentMonthBorderColor), typeof(Color), typeof(CalendarView));
        public static readonly BindableProperty DayTodayTextColorProperty = BindableProperty.Create(nameof(DayTodayTextColor), typeof(Color), typeof(CalendarView), Color.White);
        public static readonly BindableProperty DayTodayBackgroundColorProperty = BindableProperty.Create(nameof(DayTodayBackgroundColor), typeof(Color), typeof(CalendarView), Color.Transparent);
        public static readonly BindableProperty DayTodayBorderColorProperty = BindableProperty.Create(nameof(DayTodayBorderColor), typeof(Color), typeof(CalendarView), Color.FromHex("#E00000"));
        public static readonly BindableProperty DayOtherMonthTextColorProperty = BindableProperty.Create(nameof(DayOtherMonthTextColor), typeof(Color), typeof(CalendarView), Color.FromHex("#A0A0A0"));
        public static readonly BindableProperty DayOtherMonthBackgroundColorProperty = BindableProperty.Create(nameof(DayOtherMonthBackgroundColor), typeof(Color), typeof(CalendarView), Color.Transparent);
        public static readonly BindableProperty DayOtherMonthBorderColorProperty = BindableProperty.Create(nameof(DayOtherMonthBorderColor), typeof(Color), typeof(CalendarView));
        public static readonly BindableProperty DayOutOfRangeTextColorProperty = BindableProperty.Create(nameof(DayOutOfRangeTextColor), typeof(Color), typeof(CalendarView), Color.FromHex("#FFA0A0"));
        public static readonly BindableProperty DayOutOfRangeBackgroundColorProperty = BindableProperty.Create(nameof(DayOutOfRangeBackgroundColor), typeof(Color), typeof(CalendarView), Color.Transparent);
        public static readonly BindableProperty DayOutOfRangeBorderColorProperty = BindableProperty.Create(nameof(DayOutOfRangeBorderColor), typeof(Color), typeof(CalendarView));
        public static readonly BindableProperty DaySelectedTextColorProperty = BindableProperty.Create(nameof(DaySelectedTextColor), typeof(Color), typeof(CalendarView), Color.White);
        public static readonly BindableProperty DaySelectedBackgroundColorProperty = BindableProperty.Create(nameof(DaySelectedBackgroundColor), typeof(Color), typeof(CalendarView), Color.FromHex("#E00000"));
        public static readonly BindableProperty DaySelectedBorderColorProperty = BindableProperty.Create(nameof(DaySelectedBorderColor), typeof(Color), typeof(CalendarView));
        public static readonly BindableProperty DayNameTextColorProperty = BindableProperty.Create(nameof(DayNameTextColor), typeof(Color), typeof(CalendarView), Color.Black);
        private static readonly BindablePropertyKey DayNamesOrderPropertyKey = BindableProperty.CreateReadOnly(nameof(DayNamesOrder), typeof(ReadOnlyObservableCollection<DayOfWeek>), typeof(CalendarView), null, defaultValueCreator: DayNamesOrderDefaultValueCreator, propertyChanged: DayNamesOrderPropertyChanged);
        public static readonly BindableProperty DayNamesOrderProperty = DayNamesOrderPropertyKey.BindableProperty;
        private static readonly BindablePropertyKey StartOfWeekDayNamesOrderPropertyKey = BindableProperty.CreateReadOnly(nameof(StartOfWeekDayNamesOrder), typeof(ReadOnlyObservableCollection<DayOfWeek>), typeof(CalendarView), null, defaultValueCreator: StartOfWeekDayNamesOrderDefaultValueCreator);
        public static readonly BindableProperty StartOfWeekDayNamesOrderProperty = StartOfWeekDayNamesOrderPropertyKey.BindableProperty;
        public static readonly BindableProperty CustomDayNamesOrderProperty = BindableProperty.Create(nameof(CustomDayNamesOrder), typeof(ObservableRangeCollection<DayOfWeek>), typeof(CalendarView), defaultValueCreator: CustomDayNamesOrderDefaultValueCreator, propertyChanged: CustomDayNamesOrderPropertyChanged);
        public static readonly BindableProperty UseCustomDayNamesOrderProperty = BindableProperty.Create(nameof(UseCustomDayNamesOrder), typeof(bool), typeof(CalendarView), false, propertyChanged: UseCustomDayNamesOrderPropertyChanged);
        public static readonly BindableProperty DayNamesTemplateProperty = BindableProperty.Create(nameof(DayNamesTemplate), typeof(ControlTemplate), typeof(CalendarView));
        public static readonly BindableProperty DayNamesHeightRequestProperty = BindableProperty.Create(nameof(DayNamesHeightRequest), typeof(double), typeof(CalendarView), 25d);
        public static readonly BindableProperty DayNameTemplateProperty = BindableProperty.Create(nameof(DayNameTemplate), typeof(DataTemplate), typeof(CalendarView));
        public static readonly BindableProperty DayNameVerticalSpacingProperty = BindableProperty.Create(nameof(DayNameVerticalSpacing), typeof(double), typeof(CalendarView));
        public static readonly BindableProperty DayNameHorizontalSpacingProperty = BindableProperty.Create(nameof(DayNameHorizontalSpacing), typeof(double), typeof(CalendarView));
        public static readonly BindableProperty MonthViewTemplateProperty = BindableProperty.Create(nameof(MonthViewTemplate), typeof(ControlTemplate), typeof(CalendarView));
        public static readonly BindableProperty MonthViewHeightRequestProperty = BindableProperty.Create(nameof(MonthViewHeightRequest), typeof(double), typeof(CalendarView), 300d);
        public static readonly BindableProperty NavigationTemplateProperty = BindableProperty.Create(nameof(NavigationTemplate), typeof(ControlTemplate), typeof(CalendarView));
        public static readonly BindableProperty NavigationHeightRequestProperty = BindableProperty.Create(nameof(NavigationHeightRequest), typeof(double), typeof(CalendarView), 40d);
        public static readonly BindableProperty NavigationBackgroundColorProperty = BindableProperty.Create(nameof(NavigationBackgroundColor), typeof(Color), typeof(CalendarView), Color.FromHex("#E00000"));
        public static readonly BindableProperty NavigationTextColorProperty = BindableProperty.Create(nameof(NavigationTextColor), typeof(Color), typeof(CalendarView), Color.White);
        public static readonly BindableProperty NavigationArrowColorProperty = BindableProperty.Create(nameof(NavigationArrowColor), typeof(Color), typeof(CalendarView), Color.White);
        public static readonly BindableProperty NavigationArrowBackgroundColorProperty = BindableProperty.Create(nameof(NavigationArrowBackgroundColor), typeof(Color), typeof(CalendarView), Color.Transparent);
        public static readonly BindableProperty NavigationArrowCornerRadiusProperty = BindableProperty.Create(nameof(NavigationArrowCornerRadius), typeof(float), typeof(CalendarView), 100f);
        public static readonly BindableProperty NavigationLoopModeProperty = BindableProperty.Create(nameof(NavigationLoopMode), typeof(NavigationLoopMode), typeof(CalendarView), NavigationLoopMode.LoopMinimumAndMaximum);
        public static readonly BindableProperty NavigationTimeUnitProperty = BindableProperty.Create(nameof(NavigationTimeUnit), typeof(NavigationTimeUnit), typeof(CalendarView), NavigationTimeUnit.Month);
        public static readonly BindableProperty ForwardsNavigationAmountProperty = BindableProperty.Create(nameof(ForwardsNavigationAmount), typeof(int), typeof(CalendarView), 1);
        public static readonly BindableProperty BackwardsNavigationAmountProperty = BindableProperty.Create(nameof(BackwardsNavigationAmount), typeof(int), typeof(CalendarView), -1);
        public static readonly BindableProperty PageStartModeProperty = BindableProperty.Create(nameof(PageStartMode), typeof(PageStartMode), typeof(CalendarView), PageStartMode.FirstDayOfMonth, propertyChanged: PageStartModePropertyChanged);
        public static readonly BindableProperty ClampNavigationToDayRangeProperty = BindableProperty.Create(nameof(ClampNavigationToDayRange), typeof(bool), typeof(CalendarView), true, propertyChanged: ClampNavigationToDayRangePropertyChanged);
        #endregion

        #endregion

        #endregion

        #region Commands
        /// <summary>
        /// The command used to navigate the calendar.
        /// </summary>
        public ICommand NavigateCalendarCommand { get; private set; }
        /// <summary>
        /// The command used to change the date selection.
        /// </summary>
        public ICommand ChangeDateSelectionCommand { get; private set; }
        #endregion

        #region Events
        public event EventHandler<DateSelectionChangedEventArgs> DateSelectionChanged;
        public event EventHandler MonthViewDaysInvalidated;
        #endregion

        #region Constructors
        public CalendarView()
        {
            NavigateCalendarCommand = new Command<int>(NavigateCalendar);
            ChangeDateSelectionCommand = new Command<DateTime>(ChangeDateSelection);

            List<DayOfWeek> InitialStartOfWeekDayNamesOrder = new List<DayOfWeek>();
            for (int i = 0; i < DaysOfWeek.Count; i++)
            {
                int Offset = DaysOfWeek.IndexOf(StartOfWeek);
                int DayNameIndex = (i + Offset) < DaysOfWeek.Count ? (i + Offset) : (i + Offset) - DaysOfWeek.Count;
                InitialStartOfWeekDayNamesOrder.Add(DaysOfWeek[DayNameIndex]);
            }
            _StartOfWeekDayNamesOrder.ReplaceRange(InitialStartOfWeekDayNamesOrder);

            InitializeComponent();
            UpdateMonthViewDates(NavigatedDate);
            OnMonthViewDaysInvalidated();
        }
        #endregion

        #region Methods
        /// <remarks>
        /// Called when <see cref="SelectedDates"/> changes.
        /// </remarks>
        public void OnDateSelectionChanged(IList<DateTime> OldSelection, IList<DateTime> NewSelection)
        {
            DateSelectionChanged?.Invoke(this, new DateSelectionChangedEventArgs(OldSelection, NewSelection));
        }
        /// <summary>
        /// Called when the <see cref="CalendarView"/> needs to notify <see cref="CalendarDayView"/>s to reevaluate their properties due to a change.
        /// </summary>
        public void OnMonthViewDaysInvalidated()
        {
            MonthViewDaysInvalidated?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Performs selection of a <see cref="DateTime"/> depending on the current <see cref="SelectionMode"/> and <see cref="SelectionType"/>.
        /// </summary>
        /// <param name="DateTime">The <see cref="DateTime"/> to select/unselect.</param>
        public virtual void ChangeDateSelection(DateTime DateTime)
        {
            switch (SelectionType)
            {
                case SelectionType.None:
                    break;

                case SelectionType.Single:
                    switch (SelectionMode)
                    {
                        case Enums.SelectionMode.Add:
                            if (!SelectedDates.Any(x => x.Date == DateTime.Date))
                            {
                                SelectedDates.Add(DateTime.Date);
                            }
                            break;

                        case Enums.SelectionMode.Remove:
                            if (SelectedDates.Any(x => x.Date == DateTime.Date))
                            {
                                SelectedDates.Remove(DateTime.Date);
                            }
                            break;

                        case Enums.SelectionMode.Modify:
                            if (SelectedDates.Any(x => x.Date == DateTime.Date))
                            {
                                SelectedDates.Remove(DateTime.Date);
                            }
                            else
                            {
                                SelectedDates.Add(DateTime.Date);
                            }
                            break;

                        case Enums.SelectionMode.Replace:
                            if (SelectedDates.Count != 1 || (SelectedDates.Count == 1 && SelectedDates.First().Date != DateTime.Date))
                            {
                                SelectedDates.Replace(DateTime.Date);
                            }
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case SelectionType.Range:
                    if (RangeSelectionStart == null)
                    {
                        RangeSelectionStart = DateTime;
                    }
                    else if (DateTime == RangeSelectionStart)
                    {
                        RangeSelectionStart = null;
                    }
                    else if (DateTime != RangeSelectionStart)
                    {
                        RangeSelectionEnd = DateTime;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        ///  Performs selection on a range of dates as defined by <see cref="RangeSelectionStart"/> and <see cref="RangeSelectionEnd"/> depending on the current <see cref="SelectionType"/>.
        /// </summary>
        public virtual void CommitRangeSelection()
        {
            if (RangeSelectionStart == null || RangeSelectionEnd == null) { throw new InvalidOperationException($"{nameof(RangeSelectionStart)} and {nameof(RangeSelectionEnd)} must not be null."); }

            List<DateTime> DateRange = RangeSelectionStart.Value.GetDatesBetween(RangeSelectionEnd.Value);
            IEnumerable<DateTime> DatesToAdd = DateRange.Where(x => !SelectedDates.Contains(x.Date));
            IEnumerable<DateTime> DatesToRemove = DateRange.Where(x => SelectedDates.Contains(x.Date));

            switch (SelectionMode)
            {
                case Enums.SelectionMode.Add:
                    if (DatesToAdd.Count() != 0)
                    {
                        SelectedDates.AddRange(DatesToAdd);
                    }
                    break;

                case Enums.SelectionMode.Remove:
                    if (DatesToRemove.Count() != 0)
                    {
                        SelectedDates.RemoveRange(DatesToRemove);
                    }
                    break;

                case Enums.SelectionMode.Modify:
                    if (DatesToAdd.Count() != 0 || DatesToRemove.Count() != 0)
                    {
                        List<DateTime> NewSelectedDates = SelectedDates.Where(x => !DatesToRemove.Contains(x.Date)).ToList();
                        NewSelectedDates.AddRange(DatesToAdd);
                        SelectedDates.ReplaceRange(NewSelectedDates);
                    }
                    break;

                case Enums.SelectionMode.Replace:
                    if (SelectedDates.SequenceEqual(DateRange))
                    {
                        SelectedDates.Clear();
                    }
                    else
                    {
                        SelectedDates.ReplaceRange(DateRange);
                    } 
                    break;

                default:
                    throw new NotImplementedException();
            }

            RangeSelectionStart = null;
            RangeSelectionEnd = null;
        }
        /// <summary>
        /// Gets the number of rows needed to display the days of a month based on how many weeks the months has.
        /// </summary>
        /// <param name="DateTime">The <see cref="DateTime"/> representing the month to get the number of rows for.</param>
        /// <param name="IsConsistent">Whether the return value should be the highest possible value occuring in the year or not.</param>
        /// <param name="StartOfWeek">The start of the week.</param>
        /// <returns></returns>
        public static int GetMonthRows(DateTime DateTime, bool IsConsistent, DayOfWeek StartOfWeek)
        {
            if (IsConsistent)
            {
                return DateTime.CalendarHighestMonthWeekCountOfYear(StartOfWeek);
            }
            else
            {
                return DateTime.CalendarWeeksInMonth(StartOfWeek);
            }
        }
        /// <summary>
        /// Updates the dates displayed on the calendar.
        /// </summary>
        /// <param name="NavigationDate">The <see cref="DateTime"/> who's month will be used to update the dates.</param>
        public void UpdateMonthViewDates(DateTime NavigationDate)
        {
            List<DayOfWeek> DayNamesOrderList = new List<DayOfWeek>(DayNamesOrder);
            int DatesUpdated = 0;
            int RowsRequiredToNavigate = AutoRows ? GetMonthRows(NavigationDate, AutoRowsIsConsistent, StartOfWeek) : Rows;
            int DaysRequiredToNavigate = RowsRequiredToNavigate * DayNamesOrder.Count;

            //Determine the starting date of the page.
            DateTime PageStartDate;
            switch (PageStartMode)
            {
                case PageStartMode.FirstDayOfWeek:
                    PageStartDate = NavigationDate.FirstDayOfWeek(StartOfWeek);
                    break;
                case PageStartMode.FirstDayOfMonth:
                    PageStartDate = NavigationDate.FirstDayOfMonth().FirstDayOfWeek(StartOfWeek);
                    break;
                case PageStartMode.FirstDayOfYear:
                    PageStartDate = new DateTime(NavigatedDate.Year, 1, 1).FirstDayOfWeek(StartOfWeek);
                    break;
                default:
                    throw new NotImplementedException($"{nameof(Enums.PageStartMode)} '{PageStartMode}' has not been implemented.");
            }

            //Add/Remove days until reaching the required count.
            while (DaysRequiredToNavigate - Days.Count != 0)
            {
                if (DaysRequiredToNavigate - Days.Count > 0)
                {
                    _Days.Add(new CalendarDay());
                }
                else
                {
                    _Days.RemoveAt(Days.Count - 1);
                }
            }

            //Update the dates for each row.
            for (int RowsAdded = 0; DatesUpdated < DaysRequiredToNavigate; RowsAdded++)
            {
                Dictionary<DayOfWeek, DateTime> Row = new Dictionary<DayOfWeek, DateTime>();

                //Get the updated dates for the row.
                for (int i = 0; i < DaysOfWeek.Count; i++)
                {
                    try
                    {
                        DateTime DateTime = PageStartDate.AddDays(RowsAdded * DaysOfWeek.Count + i);
                        Row.Add(DateTime.DayOfWeek, DateTime);
                    }
                    catch (ArgumentOutOfRangeException Ex) when (Ex.TargetSite.DeclaringType == typeof(DateTime))
                    {
                    }
                }

                //Update the dates in the row based on the DayNamesOrder.
                for (int i = 0; i < DayNamesOrderList.Count; i++)
                {
                    try
                    {
                        Days[DatesUpdated].DateTime = Row[DayNamesOrderList[i]];
                    }
                    catch (KeyNotFoundException)
                    {
                        //Catch for when RowDates may not have a certain DayOfWeek, for example when the week spans into unrepresentable DateTimes.
                        Days[DatesUpdated].DateTime = DateTime.MaxValue;
                    }

                    DatesUpdated += 1;
                }
            }
        }
        /// <summary>
        /// Navigates the date at which the time unit is extracted.
        /// </summary>
        /// <param name="Forward">Whether the source will be navigated forwards or backwards</param>
        /// <exception cref="NotImplementedException">The current <see cref="PageStartMode"/> is not implemented</exception>
        public virtual void NavigateCalendar(int Amount)
        {
            DateTime MinimumDate = ClampNavigationToDayRange ? DayRangeMinimumDate : DateTime.MinValue;
            DateTime MaximumDate = ClampNavigationToDayRange ? DayRangeMaximumDate : DateTime.MaxValue;

            NavigatedDate = NavigateDateTime(NavigatedDate, MinimumDate, MaximumDate, Amount, NavigationLoopMode, NavigationTimeUnit, StartOfWeek);
        }
        /// <summary>
        /// Performs navigation on a DateTime.
        /// </summary>
        /// <param name="DateTime">The <see cref="DateTime"/> that will be the source of the navigation.</param>
        /// <param name="MinimumDate">The lower bound of the range of dates. Inclusive.</param>
        /// <param name="MaximumDate">The upper bound of the range of dates. Inclusive.</param>
        /// <param name="Amount">The amount of the <paramref name="NavigationTimeUnit"/> to navigate.</param>
        /// <param name="NavigationLoopMode">What to do when the result of navigation is outside the range of the <paramref name="MinimumDate"/> and <paramref name="MaximumDate"/>.</param>
        /// <param name="NavigationTimeUnit">The time unit to navigate the <paramref name="DateTime"/> by.</param>
        /// <param name="StartOfWeek">The start of the week.</param>
        /// <returns>The <see cref="DateTime"/> resulting from the navigation.</returns>
        /// <exception cref="NotImplementedException">The <see cref="NavigationTimeUnit"/> is not implemented.</exception>
        public DateTime NavigateDateTime(DateTime DateTime, DateTime MinimumDate, DateTime MaximumDate, int Amount, NavigationLoopMode NavigationLoopMode, NavigationTimeUnit NavigationTimeUnit, DayOfWeek StartOfWeek)
        {
            bool LowerThanMinimumDate;
            bool HigherThanMaximumDate;

            DateTime NewNavigatedDate;

            try
            {
                switch (NavigationTimeUnit)
                {
                    case NavigationTimeUnit.Day:
                        NewNavigatedDate = DateTime.AddDays(Amount);
                        break;

                    case NavigationTimeUnit.Week:
                        NewNavigatedDate = DateTime.AddWeeks(Amount);
                        break;

                    case NavigationTimeUnit.Month:
                        NewNavigatedDate = DateTime.AddMonths(Amount);
                        break;

                    case NavigationTimeUnit.Year:
                        NewNavigatedDate = DateTime.AddYears(Amount);
                        break;

                    default:
                        throw new NotImplementedException($"{nameof(Enums.NavigationTimeUnit)} '{NavigationTimeUnit}' is not implemented.");
                }

                LowerThanMinimumDate = NewNavigatedDate.Date < MinimumDate.Date;
                HigherThanMaximumDate = NewNavigatedDate.Date > MaximumDate.Date;
            }
            catch (ArgumentOutOfRangeException Ex) when (Ex.TargetSite.DeclaringType == typeof(DateTime))
            {
                NewNavigatedDate = Amount > 0 ? MaximumDate : MinimumDate;
                LowerThanMinimumDate = Amount < 0;
                HigherThanMaximumDate = Amount > 0;
            }

            if (LowerThanMinimumDate && NavigationLoopMode.HasFlag(NavigationLoopMode.LoopMinimum))
            {
                NewNavigatedDate = MaximumDate;
                //The code below makes sure that the correct amount of weeks are added after looping.
                //However this is not possible when setting the NavigatedDate directly, so it is commented out for the sake of consistency.
                    
                ////The difference in weeks must be made consistent because NavigatedDate could be any value within the week.
                ////The minimum date may not always have the first day of week so the last day of week is used to do this.
                //TimeSpan Difference = DateTime.LastDayOfWeek(StartOfWeek) - MinimumDate.LastDayOfWeek(StartOfWeek);

                //int WeeksUntilMinValue = (int)Math.Ceiling(Difference.TotalDays / 7);
                //DateTime NewNavigatedDate = NavigateDateTime(MinimumDate, MinimumDate, MaximumDate, Amount + WeeksUntilMinValue, NavigationLoopMode, StartOfWeek);


                ////Preserve the original time.
                //return new DateTime(NewNavigatedDate.Year, NewNavigatedDate.Month, NewNavigatedDate.Day, DateTime.Hour, DateTime.Minute, DateTime.Second, DateTime.Millisecond);
            }
            else if (HigherThanMaximumDate && NavigationLoopMode.HasFlag(NavigationLoopMode.LoopMaximum))
            {
                NewNavigatedDate = MinimumDate;
                //The code below makes sure that the correct amount of weeks are added after looping.
                //However this is not possible when setting the NavigatedDate directly, so it is commented out for the sake of consistency.
                    
                ////The difference in weeks must be made consistent because NavigatedDate could be any value within the week.
                ////The maximum date may not always have the last day of week so the first day of week is used to do this.
                //TimeSpan Difference = MaximumDate.FirstDayOfWeek(StartOfWeek) - DateTime.FirstDayOfWeek(StartOfWeek);

                //int WeeksUntilMaxValue = (int)Math.Ceiling(Difference.TotalDays / 7);
                //DateTime NewNavigatedDate = NavigateDateTime(MinimumDate, MinimumDate, MaximumDate, Amount - WeeksUntilMaxValue, NavigationLoopMode, StartOfWeek);

                ////Preserve the original time.
                //return new DateTime(NewNavigatedDate.Year, NewNavigatedDate.Month, NewNavigatedDate.Day, DateTime.Hour, DateTime.Minute, DateTime.Second, DateTime.Millisecond);
            }

            return NewNavigatedDate;
        }
        private void SelectedDates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnMonthViewDaysInvalidated();

            OnDateSelectionChanged(_PreviousSelectedDates, SelectedDates);

            _PreviousSelectedDates.Clear();
            _PreviousSelectedDates.AddRange(SelectedDates.ToList());
        }
        private void DayNamesOrder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DayNamesOrder.Count == 0) { throw new InvalidOperationException($"{nameof(DayNamesOrder)} must contain at least one {nameof(DayOfWeek)}."); }
            UpdateMonthViewDates(NavigatedDate);
            OnMonthViewDaysInvalidated();
        }

        #region Bindable Properties Methods
        private static void RowsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            int CoercedRows = (int)GetCorrectRows(Control, Control.Rows);
            if (Control.Rows != CoercedRows)
            {
                Control.Rows = CoercedRows;
            }
            else
            {
                Control.UpdateMonthViewDates(Control.NavigatedDate);
                Control.OnMonthViewDaysInvalidated();
            }
        }
        private static void NavigatedDatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            int CoercedRows = (int)GetCorrectRows(Control, Control.Rows);

            if (Control.Rows == CoercedRows)
            {
                Control.UpdateMonthViewDates(Control.NavigatedDate);
                Control.OnMonthViewDaysInvalidated();
            }
            else
            {
                Control.Rows = CoercedRows;
            }
        }
        private static void ClampNavigationToDayRangePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;
            Control.NavigatedDate = (DateTime)CoerceNavigatedDate(Control, Control.NavigatedDate);
            //Control.CoerceValue(NavigatedDateProperty);
        }
        private static void TodayDatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;
            Control.OnMonthViewDaysInvalidated();
        }
        private static void StartOfWeekPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            List<DayOfWeek> NewStartOfWeekDayNamesOrder = new List<DayOfWeek>();
            for (int i = 0; i < DaysOfWeek.Count; i++)
            {
                int Offset = DaysOfWeek.IndexOf(Control.StartOfWeek);
                int DayNameIndex = (i + Offset) < DaysOfWeek.Count ? (i + Offset) : (i + Offset) - DaysOfWeek.Count;
                NewStartOfWeekDayNamesOrder.Add(DaysOfWeek[DayNameIndex]);
            }
            Control._StartOfWeekDayNamesOrder.ReplaceRange(NewStartOfWeekDayNamesOrder);

            if (Control.UseCustomDayNamesOrder)
            {
                Control.UpdateMonthViewDates(Control.NavigatedDate);
                Control.OnMonthViewDaysInvalidated();
            }
            else
            {
                Control.DayNamesOrder = new ReadOnlyObservableCollection<DayOfWeek>(Control._StartOfWeekDayNamesOrder);
            }
        }
        private static void DayRangeMinimumDatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;
            Control.NavigatedDate = (DateTime)CoerceNavigatedDate(Control, Control.NavigatedDate);
        }
        private static void DayRangeMaximumDatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;
            Control.NavigatedDate = (DateTime)CoerceNavigatedDate(Control, Control.NavigatedDate);
        }
        private static void SelectedDatesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;
            ObservableRangeCollection<DateTime> OldSelectedDates = (ObservableRangeCollection<DateTime>)oldValue;
            ObservableRangeCollection<DateTime> NewSelectedDates = (ObservableRangeCollection<DateTime>)newValue;

            if (OldSelectedDates != null) { OldSelectedDates.CollectionChanged -= Control.SelectedDates_CollectionChanged; }
            if (NewSelectedDates != null) { NewSelectedDates.CollectionChanged += Control.SelectedDates_CollectionChanged; }

            Control._PreviousSelectedDates.Clear();
            Control._PreviousSelectedDates.AddRange(OldSelectedDates);

            if (!OldSelectedDates.SequenceEqual(NewSelectedDates))
            {
                Control.OnMonthViewDaysInvalidated();
                Control.OnDateSelectionChanged(Control._PreviousSelectedDates, NewSelectedDates);
            }
        }
        private static void CustomDayNamesOrderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            if (Control.UseCustomDayNamesOrder)
            {
                Control.DayNamesOrder = new ReadOnlyObservableCollection<DayOfWeek>(Control.CustomDayNamesOrder);
            }
        }
        private static void DayNamesOrderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;
            ReadOnlyObservableCollection<DayOfWeek> OldDayNamesOrder = (ReadOnlyObservableCollection<DayOfWeek>)oldValue;
            ReadOnlyObservableCollection<DayOfWeek> NewDayNamesOrder = (ReadOnlyObservableCollection<DayOfWeek>)newValue;

            if (OldDayNamesOrder != null) { ((INotifyCollectionChanged)OldDayNamesOrder).CollectionChanged -= Control.DayNamesOrder_CollectionChanged; }
            if (NewDayNamesOrder != null) { ((INotifyCollectionChanged)NewDayNamesOrder).CollectionChanged += Control.DayNamesOrder_CollectionChanged; }

            if (OldDayNamesOrder == null || !NewDayNamesOrder.SequenceEqual(OldDayNamesOrder))
            {
                Control.UpdateMonthViewDates(Control.NavigatedDate);
                Control.OnMonthViewDaysInvalidated();
            }
        }
        private static void AutoRowsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            Control.Rows = (int)GetCorrectRows(Control, Control.Rows);
            //Control.CoerceValue(RowsProperty);
        }
        private static void AutoRowsIsConsistentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            Control.Rows = (int)GetCorrectRows(Control, Control.Rows);
            //Control.CoerceValue(RowsProperty);
        }
        private static void UseCustomDayNamesOrderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            if ((bool)newValue)
            {
                Control.DayNamesOrder = new ReadOnlyObservableCollection<DayOfWeek>(Control.CustomDayNamesOrder);
            }
            else
            {
                Control.DayNamesOrder = new ReadOnlyObservableCollection<DayOfWeek>(Control._StartOfWeekDayNamesOrder);
            }
        }
        private static void PageStartModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;
            Control.UpdateMonthViewDates(Control.NavigatedDate);
            Control.OnMonthViewDaysInvalidated();
        }
        private static void RangeSelectionStartPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            if (Control.RangeSelectionStart != null && Control.RangeSelectionEnd != null)
            {
                Control.CommitRangeSelection();
            }
        }
        private static void RangeSelectionEndPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CalendarView Control = (CalendarView)bindable;

            if (Control.RangeSelectionStart != null && Control.RangeSelectionEnd != null)
            {
                Control.CommitRangeSelection();
            }
        }
        private static object CoerceNavigatedDate(BindableObject bindable, object value)
        {
            DateTime InitialValue = (DateTime)value;
            CalendarView Control = (CalendarView)bindable;

            DateTime MinimumDate = Control.ClampNavigationToDayRange ? Control.DayRangeMinimumDate : DateTime.MinValue;
            DateTime MaximumDate = Control.ClampNavigationToDayRange ? Control.DayRangeMaximumDate : DateTime.MaxValue;

            switch (Control.NavigationLoopMode)
            {
                case NavigationLoopMode.DontLoop:
                    if (InitialValue.Date < MinimumDate.Date) { return MinimumDate; }
                    if (InitialValue.Date > MaximumDate.Date) { return MaximumDate; }
                    break;
                case NavigationLoopMode.LoopMinimum:
                    if (InitialValue.Date < MinimumDate.Date) { return MaximumDate; }
                    if (InitialValue.Date > MaximumDate.Date) { return MaximumDate; }
                    break;

                case NavigationLoopMode.LoopMaximum:
                    if (InitialValue.Date < MinimumDate.Date) { return MinimumDate; }
                    if (InitialValue.Date > MaximumDate.Date) { return MinimumDate; }
                    break;

                case NavigationLoopMode.LoopMinimumAndMaximum:
                    if (InitialValue.Date < MinimumDate.Date) { return MaximumDate; }
                    if (InitialValue.Date > MaximumDate.Date) { return MinimumDate; }
                    break;

                default:
                    throw new NotImplementedException($"{nameof(NavigationLoopMode)} is not implemented.");
            }

            return InitialValue;
        }
        private static object GetCorrectRows(BindableObject bindable, object value)
        {
            CalendarView Control = (CalendarView)bindable;

            return Control.AutoRows ? GetMonthRows(Control.NavigatedDate, Control.AutoRowsIsConsistent, Control.StartOfWeek) : value;
        }
        private static object StartOfWeekDayNamesOrderDefaultValueCreator(BindableObject bindable)
        {
            CalendarView Control = (CalendarView)bindable;
            return new ReadOnlyObservableCollection<DayOfWeek>(Control._StartOfWeekDayNamesOrder);
        }
        private static object CustomDayNamesOrderDefaultValueCreator(BindableObject bindable)
        {
            return new ObservableRangeCollection<DayOfWeek>(DaysOfWeek);
        }
        private static object DayNamesOrderDefaultValueCreator(BindableObject bindable)
        {
            CalendarView Control = (CalendarView)bindable;

            if (Control.UseCustomDayNamesOrder)
            {
                return new ReadOnlyObservableCollection<DayOfWeek>(Control.CustomDayNamesOrder);
            }
            else
            {
                return new ReadOnlyObservableCollection<DayOfWeek>(Control._StartOfWeekDayNamesOrder);
            }
        }
        private static object SelectedDatesDefaultValueCreator(BindableObject bindable)
        {
            CalendarView Control = (CalendarView)bindable;
            ObservableRangeCollection<DateTime> DefaultValue = new ObservableRangeCollection<DateTime>();

            DefaultValue.CollectionChanged += Control.SelectedDates_CollectionChanged;
            return DefaultValue;
        }
        private static object DaysDefaultValueCreator(BindableObject bindable)
        {
            CalendarView Control = (CalendarView)bindable;
            return new ReadOnlyObservableCollection<CalendarDay>(Control._Days);
        }
        private static bool IsRowsValidValue(BindableObject bindable, object value)
        {
            return (int)value > 0;
        }
        private static bool IsSelectedDatesValidValue(BindableObject bindable, object value)
        {
            return value != null;
        }

        #endregion

        #endregion
    }
}
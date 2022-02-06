﻿using System;
using System.Globalization;
using TurnipTracker.Shared;
using Xamarin.Forms;

namespace TurnipTracker.Converters
{
    public class LastUpdateDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            if (value is FriendStatus status)
            {
                if (status.TurnipUpdateYear == 0 || status.TurnipUpdateDayOfYear == 0)
                    return string.Empty;
                var dt = new DateTime(status.TurnipUpdateYear, 1, 1).AddDays(status.TurnipUpdateDayOfYear - 1);

                var island = dt.ToShortDateString();
                return $"{island}  @{status.TurnipUpdateTimeUTC.ToLocalTime().ToShortTimeString()}";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KusinaPOS.Converters
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? "Available" : "Unavailable";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (string)value == "Available";
    }
}

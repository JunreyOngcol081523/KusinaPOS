using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KusinaPOS.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && parameter != null)
                return Enum.Parse(targetType, parameter.ToString());

            return Binding.DoNothing;
        }
    }
}

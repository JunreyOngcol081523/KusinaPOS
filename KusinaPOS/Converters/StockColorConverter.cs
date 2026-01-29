using KusinaPOS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KusinaPOS.Converters
{
    public class StockColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 'value' is the entire InventoryItem object passed from the Row
            if (value is InventoryItem item)
            {
                if (item.IsLowStock)
                    return Color.FromArgb("#FFEBEB"); // Soft Red for low stock

                return Colors.Transparent; // Default/Normal
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

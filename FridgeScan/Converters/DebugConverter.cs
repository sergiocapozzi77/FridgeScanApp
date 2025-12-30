using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace FridgeScan.Converters
{
    public class DebugConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"[DEBUG CONVERTER] CommandParameter = {value}");

            // You can also inspect properties:
            if (value is FridgeScan.Models.Product p)
            {
                Debug.WriteLine($"   -> Product: {p.Name}, Qty: {p.Quantity}, Type: {p.Category}");
            }

            // IMPORTANT: return the same value, so app works normally
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}

using System;
using System.Globalization;
using FridgeScan.Helpers;
using Microsoft.Maui.Controls;

namespace FridgeScan.Converters
{
    public class DebugConverter : IValueConverter
    {
        private const string Tag = "FridgeScan.DebugConverter";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Logger.Debug(Tag, $"CommandParameter = {value}");

            // You can also inspect properties:
            if (value is FridgeScan.Models.Product p)
            {
                Logger.Debug(Tag, $"  -> Product: {p.Name}, Qty: {p.Quantity}, Type: {p.Category}");
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

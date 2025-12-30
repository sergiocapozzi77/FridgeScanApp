using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Converters
{
    public class ExpandCollapseIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "\ue313";
            }
            else if ((bool)value)
            {
                return "\ue316";
            }
            else
            {
                return "\ue313";
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FoodSelectionIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "\ue717";
            }
            if ((bool)value)
            {
                return "\ue789";
            }
            else
            {
                return "\ue717";
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FoodSelectionIconColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Application.Current!.RequestedTheme == AppTheme.Light ? Color.FromArgb("#666666") : Color.FromArgb("#C4CAD0");
            }
            if ((bool)value)
            {
                return Application.Current!.RequestedTheme == AppTheme.Light ? Color.FromArgb("#6750A4") : Color.FromArgb("#D0BCFF");
            }
            else
            {
                return Application.Current!.RequestedTheme == AppTheme.Light ? Color.FromArgb("#666666") : Color.FromArgb("#C4CAD0");
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

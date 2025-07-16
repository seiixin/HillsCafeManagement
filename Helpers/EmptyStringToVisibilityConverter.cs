using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HillsCafeManagement.Helpers
{
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // When text length > 0 → hide (Collapsed)
            if (value is int length && length > 0)
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

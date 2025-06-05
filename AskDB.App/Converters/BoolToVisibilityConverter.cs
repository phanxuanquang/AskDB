using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isVisible)
            {
                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            throw new InvalidCastException($"Cannot convert from {value} to Visibility data type");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }

            throw new InvalidCastException($"Cannot convert from {value} to Boolean data type");
        }
    }
}

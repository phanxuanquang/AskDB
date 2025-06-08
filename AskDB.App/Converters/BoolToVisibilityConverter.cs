using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool isVisible && isVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException("ConvertBack is not implemented for BoolToVisibilityConverter.");
    }
}

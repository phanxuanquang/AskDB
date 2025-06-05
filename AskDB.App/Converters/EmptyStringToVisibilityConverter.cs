using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is string stringValue && (string.IsNullOrWhiteSpace(stringValue) || string.IsNullOrEmpty(stringValue))
                ? Visibility.Collapsed
                : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}

using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class BoolToNegativeBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool boolValue && !boolValue;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is bool boolValue && !boolValue;
    }
}

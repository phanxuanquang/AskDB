using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class BoolToNegativeBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            throw new InvalidOperationException("Unsupported data type");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            throw new InvalidOperationException("The data type is not supported");
        }
    }
}

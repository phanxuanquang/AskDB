using GeminiDotNET;
using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class StringToIsValidApiKey : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string apiKey)
            {
                return Validator.CanBeValidApiKey(apiKey);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

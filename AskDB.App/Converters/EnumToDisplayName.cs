using AskDB.Commons.Extensions;
using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class EnumToDisplayName : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Enum isFromUser)
            {
                return isFromUser.GetFriendlyName();
            }

            throw new ArgumentException("Value must be an Enum type", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AskDB.App.Converters
{
    public partial class BoolToHorizontalAlignment : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
          => value is bool isFromUser && isFromUser
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}

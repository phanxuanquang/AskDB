using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;

namespace AskDB.App.Converters
{
    public partial class IEnumerableToVisibilityConverter : IValueConverter
    {
        public bool Inverse { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IList list)
            {
                bool hasItems = list.Count > 0;

                if (Inverse)
                    hasItems = !hasItems;

                return hasItems ? Visibility.Visible : Visibility.Collapsed;
            }

            return Inverse ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

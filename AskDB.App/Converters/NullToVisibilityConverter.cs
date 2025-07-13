using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Data;

namespace AskDB.App.Converters
{
    public partial class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (value is string s)
            {
                return string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                return !enumerable.GetEnumerator().MoveNext() ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is DataTable dataTable)
            {
                return dataTable.Rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

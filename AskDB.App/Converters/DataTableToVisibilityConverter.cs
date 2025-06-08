using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Data;

namespace AskDB.App.Converters
{
    public partial class DataTableToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is DataTable table && table.Rows.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}

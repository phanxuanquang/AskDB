using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace AskDB.App.Converters
{
    public partial class DataTableToObservableCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        => value is DataTable dataTable && dataTable.Rows.Count > 0
            ? new ObservableCollection<object>(dataTable.Rows.Cast<DataRow>().Select(row => row.ItemArray))
            : [];

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

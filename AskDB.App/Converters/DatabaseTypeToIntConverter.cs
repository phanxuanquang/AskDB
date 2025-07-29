using AskDB.Commons.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskDB.App.Converters
{
    public partial class DatabaseTypeToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is DatabaseType databaseType
                ? (int)databaseType
                : throw new ArgumentException("Value must be of type DatabaseType.", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue && Enum.IsDefined(typeof(DatabaseType), intValue))
            {
                return (DatabaseType)intValue;
            }

            throw new ArgumentException("Value must be a valid integer corresponding to DatabaseType.", nameof(value));
        }
    }
}

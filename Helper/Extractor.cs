using System.ComponentModel;
using System.Data;
using System.Text;

namespace Helper
{
    public static class Extractor
    {
        public static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }

        public static void ExportData(DataTable table, string outputFilePath)
        {
            StringBuilder sb = new();

            string[] columnNames = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in table.Rows)
            {
                var fields = row.ItemArray.Select(field => field?.ToString()).ToArray();
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(outputFilePath, sb.ToString());
        }
    }
}

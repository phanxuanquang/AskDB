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

        public static void ExportCsv(DataTable table, string outputFilePath)
        {
            using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8, bufferSize: 65536); 
            var sb = new StringBuilder();

            var totalColumns = table.Columns.Count;

            for (int i = 0; i < totalColumns; i++)
            {
                sb.Append(StringTool.EscapeCsvValue(table.Columns[i].ColumnName));
                if (i < totalColumns - 1)
                {
                    sb.Append(',');
                }
            }
            writer.WriteLine(sb.ToString());

            foreach (DataRow row in table.Rows)
            {
                sb.Clear();
                for (int i = 0; i < totalColumns; i++)
                {
                    var value = row[i] != null ? StringTool.EscapeCsvValue(row[i].ToString()) : string.Empty;
                    sb.Append(value);
                    if (i < totalColumns - 1)
                    {
                        sb.Append(',');
                    }
                }
                writer.WriteLine(sb.ToString());
            }
        }

    }
}

using System.Data;
using System.Text;

namespace AskDB.Commons.Extensions
{
    public static class DataTableExtensions
    {
        public static List<string> ToListString(this DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0) return [];

            var results = new List<string>();
            foreach (DataRow row in dataTable.Rows)
            {
                var value = row[0]?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    results.Add(value);
                }
            }
            return results.Distinct().ToList();
        }

        public static string ToMarkdown(this DataTable dataTable, int maxRow = 100)
        {
            if (dataTable == null || dataTable.Rows.Count == 0) return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                sb.Append("| " + dataTable.Columns[i].ColumnName + " ");
            }
            sb.AppendLine("|");

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                sb.Append("| --- ");
            }
            sb.AppendLine("|");

            int count = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                count++;
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    sb.Append("| " + row[i].ToString() + " ");
                }
                sb.AppendLine("|");

                if (count > maxRow)
                {
                    sb.AppendLine();
                    sb.AppendLine($"> **Important:** The table above shows only {count}/{dataTable.Rows.Count} records due to your context lenght limitation. You should review your SQL query to avoid retrieving too many rows unnecessarily and to narrow down the records");
                    break;
                }
            }

            return sb.ToString();
        }

        public static async Task ToCsvAsync(this DataTable dataTable, string filePath)
        {
            ArgumentNullException.ThrowIfNull(dataTable);

            var sb = new StringBuilder();

            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                sb.Append(EscapeCsvField(dataTable.Columns[i].ColumnName));
                if (i < dataTable.Columns.Count - 1) sb.Append(',');
            }
            sb.AppendLine();

            foreach (DataRow row in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    sb.Append(EscapeCsvField(row[i]?.ToString()));
                    if (i < dataTable.Columns.Count - 1) sb.Append(',');
                }
                sb.AppendLine();
            }

            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            using var writer = new StreamWriter(filePath, false, utf8WithBom);
            await writer.WriteAsync(sb.ToString());
        }

        private static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;

            bool mustQuote = field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r');
            field = field.Replace("\"", "\"\"");

            return mustQuote ? $"\"{field}\"" : field;
        }
    }
}

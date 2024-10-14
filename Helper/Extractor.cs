using Octokit;
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
            using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8);
            var header = string.Join(",", table.Columns.Cast<DataColumn>().Select(col => StringTool.EscapeCsvValue(col.ColumnName)));
            writer.WriteLine(header);

            foreach (DataRow row in table.Rows)
            {
                var rowValues = string.Join(",", row.ItemArray.Select(value => StringTool.EscapeCsvValue(value.ToString())));
                writer.WriteLine(rowValues);
            }
        }

        public static async Task<Release?> GetGithubLatestReleaseInfo()
        {
            var client = new GitHubClient(new ProductHeaderValue("AskDB-Client"));
            try
            {
                return await client.Repository.Release.GetLatest("phanxuanquang", "AskDB");
            }
            catch
            {
                return null;
            }
        }
    }
}

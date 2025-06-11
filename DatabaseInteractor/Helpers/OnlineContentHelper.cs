using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;

namespace DatabaseInteractor.Helpers
{
    public static class OnlineContentHelper
    {
        private const string UrlPrefix = "https://raw.githubusercontent.com/phanxuanquang/AskDB/refs/heads/master/DatabaseInteractor";
        public static async Task<string> GetSytemInstructionContentAsync(string instructionFileName, DatabaseType databaseType, string language)
        {
            try
            {
                var url = $"{UrlPrefix}/Instructions/{instructionFileName}.md";
                var content = await GetContentFromUrlAsync(url);

                return content
                    .Replace("{Language}", language)
                    .Replace("{DateTime_Now}", DateTime.Now.ToLongDateString())
                    .Replace("{Database_Type}", databaseType.GetDescription());
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Failed to fetch content from GitHub.", ex);
            }
        }

        public static async Task<string> GeSqlContentAsync(DatabaseType databaseType, string methodName)
        {
            try
            {
                var database = databaseType.GetDescription();
                var url = $"{UrlPrefix}/SQL Queries/{methodName}/{database}.sql";
                return await GetContentFromUrlAsync(url);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Failed to fetch content from GitHub.", ex);
            }
        }

        private static async Task<string> GetContentFromUrlAsync(string url)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            return await client.GetStringAsync(new Uri(url));
        }
    }
}

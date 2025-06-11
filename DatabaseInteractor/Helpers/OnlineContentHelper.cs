using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;

namespace DatabaseInteractor.Helpers
{
    public static class OnlineContentHelper
    {
        private const string UrlPrefix = "https://raw.githubusercontent.com/phanxuanquang/AskDB/refs/heads/master/DatabaseInteractor";
        public static async Task<string> GetSytemInstructionContentAsync(string instructionFileName, DatabaseType databaseType, string language)
        {
            var url = $"{UrlPrefix}/Instructions/{instructionFileName}.md";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            try
            {
                var content = await client.GetStringAsync(url);

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
            var url = $"{UrlPrefix}/SQL Queries/{methodName}/{databaseType.GetDescription}.sql";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            try
            {
                return await client.GetStringAsync(url);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("Failed to fetch content from GitHub.", ex);
            }
        }
    }
}

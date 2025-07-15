namespace AskDB.Commons.Helpers
{
    public static class GithubOnlineContentHelper
    {
        public static async Task<string> GetContentFromUrlAsync(string url)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            return await client.GetStringAsync(new Uri(url));
        }
    }
}

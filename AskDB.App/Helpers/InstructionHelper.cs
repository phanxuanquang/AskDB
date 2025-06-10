using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AskDB.App.Helpers
{
    public static class InstructionHelper
    {
        public static async Task<string> GetGitHubRawFileContentAsync(string instructionFileName)
        {
            var url = $"https://raw.githubusercontent.com/phanxuanquang/AskDB/refs/heads/master/DatabaseInteractor/Instructions/{instructionFileName}.md";

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

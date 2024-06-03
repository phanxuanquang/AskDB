using Newtonsoft.Json;
using System.Text;
using static Gemini_API_Helper.DTO.ResponseForOneShot;

namespace Gemini_API_Helper
{
    public static class Helper
    {
        private static string ApiKey = "AIzaSyDTxvJEdHMG5a8b9z8SCuus4jgnL91_yi4";
        public static async Task<string> GetResponseFor(string question, EnumModel model = EnumModel.Gemini_10_Pro, double temperature = 0.25)
        {
            var client = new HttpClient();
            var modelName = EnumHelper.GetEnumDescription(model);
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={ApiKey}";

            var requestData = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = "Write a story about a magic backpack."
                            }
                        }
                    }
                },
                safetySettings = new[]
                {
                    new
                    {
                        category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                        threshold = "BLOCK_ONLY_HIGH"
                    }
                },
                generationConfig = new
                {
                    stopSequences = new[]
                    {
                        "Title"
                    },
                    temperature = temperature,
                    maxOutputTokens = 2048,
                    topP = 0.8,
                    topK = 10
                }
            };


            try
            {
                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadAsStringAsync();
                var dto = JsonConvert.DeserializeObject<Response>(responseData);
                return dto.Candidates[0].Content.Parts[0].Text;
            }
            catch (HttpRequestException e)
            {
                return $"Request error: {e.Message}";
            }
        }
    }
}

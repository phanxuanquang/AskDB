using Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace GenAI
{
    public static class Generator
    {
        public const string ApiKeySite = "https://aistudio.google.com/app/apikey";
        public const string ApiKeyPrefix = "AIzaSy";

        public static string ApiKey;
        private static HttpClient _client = new HttpClient();

        public static async Task<string> GenerateContent(string apiKey, string query, bool useJson = false, CreativityLevel creativityLevel = CreativityLevel.Medium, GenerativeModel model = GenerativeModel.Gemini_15_Flash)
        {
            var modelName = Extractor.GetEnumDescription(model);
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = query
                            }
                        }
                    }
                },
                safetySettings = new[]
                {
                    new
                    {
                        category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                        threshold = "BLOCK_NONE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_HARASSMENT",
                        threshold = "BLOCK_NONE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_HATE_SPEECH",
                        threshold = "BLOCK_NONE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                        threshold = "BLOCK_NONE"
                    }
                },
                generationConfig = new
                {
                    temperature = (double)creativityLevel / 100,
                    topP = 0.8,
                    topK = 10,
                    responseMimeType = useJson ? "application/json" : "text/plain"
                }
            };

            var body = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(endpoint, body).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseDTO = JsonConvert.DeserializeObject<ApiResponseDto.Response>(responseData);

            return responseDTO.Candidates[0].Content.Parts[0].Text;
        }

        public static async Task<List<string>> GenerateContentAsArray(string apiKey, string query, CreativityLevel creativityLevel = CreativityLevel.Medium, GenerativeModel model = GenerativeModel.Gemini_15_Flash)
        {
            var modelName = Extractor.GetEnumDescription(model);
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = query
                            }
                        }
                    }
                },
                safetySettings = new[]
                {
                    new
                    {
                        category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                        threshold = "BLOCK_NONE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_HARASSMENT",
                        threshold = "BLOCK_NONE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_HATE_SPEECH",
                        threshold = "BLOCK_NONE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                        threshold = "BLOCK_NONE"
                    }
                },
                generationConfig = new
                {
                    temperature = (double)creativityLevel / 100,
                    topP = 0.8,
                    topK = 10,
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            objects = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "string"
                                }
                            }
                        },
                        required = new string[] { "objects" }
                    }
                }
            };

            var body = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(endpoint, body).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseDTO = JsonConvert.DeserializeObject<ApiResponseDto.Response>(responseData);

            var jsonObj = JObject.Parse(responseDTO.Candidates[0].Content.Parts[0].Text);

            return jsonObj["objects"].ToObject<List<string>>();
        }

        public static bool CanBeGeminiApiKey(string apiKey)
        {
            if (ApiKeyPrefix.StartsWith(apiKey, StringComparison.OrdinalIgnoreCase) || apiKey.StartsWith(ApiKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static async Task<bool> IsValidApiKey(string apiKey)
        {
            try
            {
                await GenerateContent(apiKey.Trim(), "Say 'Hello World' to me!", false, CreativityLevel.Low);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
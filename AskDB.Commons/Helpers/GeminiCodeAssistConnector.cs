using AskDB.Commons.Extensions;
using GeminiDotNET.ApiModels.ApiRequest;
using GeminiDotNET.ClientModels;
using GeminiDotNET.Extensions;
using GeminiDotNET.Helpers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AskDB.Commons.Helpers
{
    public class GeminiCodeAssistConnector
    {
        // Refer to https://github.com/google-gemini/gemini-cli/blob/main/packages/core/src/code_assist/oauth2.ts

        #region Declarations
        private const string CLIENT_ID = "";
        private const string CLIENT_SECRET = "";
        private const string CredentialFileName = "DO NOT DELETE THIS.creds";
        private const string API_ENDPOINT_PREFIX = "https://cloudcode-pa.googleapis.com/v1internal";

        private static readonly string GeminiDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
        private static readonly string CredentialPath = Path.Combine(GeminiDir, CredentialFileName);

        private static readonly GoogleAuthorizationCodeFlow AuthenCodeFlow = new(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = CLIENT_ID,
                ClientSecret = CLIENT_SECRET
            },
            Scopes =
            [
                "https://www.googleapis.com/auth/cloud-platform",
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/userinfo.profile",
            ]
        });
        #endregion

        private UserCredential? _userCredential;
        private DateTime _accessTokenExpirationTime = DateTime.Now;
        private string _googleCloudProjectId;
        public string CurrentTierId { get; private set; }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_userCredential == null || _accessTokenExpirationTime < DateTime.Now)
            {
                await StartAuthenticationAsync();
            }

            return _userCredential!.Token.AccessToken;
        }

        public async Task StartAuthenticationAsync()
        {
            TokenResponse? token = null;

            if (File.Exists(CredentialPath))
            {
                var encryptedContent = await File.ReadAllTextAsync(CredentialPath);
                token = JsonSerializer.Deserialize<TokenResponse>(encryptedContent.AesDecrypt());
            }

            if (token == null)
            {
                _userCredential = await AuthenticateWithWebAsync();
                _accessTokenExpirationTime = _userCredential.Token.ExpiresInSeconds.HasValue
                    ? DateTime.Now.AddSeconds(_userCredential.Token.ExpiresInSeconds.Value).AddMinutes(-5)
                    : DateTime.MaxValue;
                return;
            }

            _userCredential = new UserCredential(AuthenCodeFlow, "user", token);

            if (!await _userCredential.RefreshTokenAsync(CancellationToken.None))
            {
                _userCredential = await AuthenticateWithWebAsync();
                _accessTokenExpirationTime = _userCredential.Token.ExpiresInSeconds.HasValue
                    ? DateTime.Now.AddSeconds(_userCredential.Token.ExpiresInSeconds.Value).AddMinutes(-5)
                    : DateTime.MaxValue;
                return;
            }

            _accessTokenExpirationTime = _userCredential.Token.ExpiresInSeconds.HasValue
                   ? DateTime.Now.AddSeconds(_userCredential.Token.ExpiresInSeconds.Value).AddMinutes(-5)
                   : DateTime.MaxValue;
        }

        public static void ClearCachedUserCredential()
        {
            if (File.Exists(CredentialPath))
            {
                File.Delete(CredentialPath);
            }

            if (Directory.Exists(GeminiDir))
            {
                Directory.Delete(GeminiDir);
            }
        }

        public async Task LoadCodeAssistAsync()
        {
            if (_userCredential == null)
            {
                throw new InvalidOperationException("You must authenticate before loading the Code Assist profile.");
            }

            var payload = new
            {
                metadata = new
                {
                    ideType = "IDE_UNSPECIFIED",
                    platform = "PLATFORM_UNSPECIFIED",
                    pluginType = "GEMINI",
                }
            };

            var methodName = "loadCodeAssist";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userCredential.Token.AccessToken);

            HttpResponseMessage? response;

            if (payload != null)
            {
                var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                response = await client.PostAsync($"{API_ENDPOINT_PREFIX}:{methodName}", body);
            }
            else
            {
                response = await client.GetAsync($"{API_ENDPOINT_PREFIX}:{methodName}");
            }

            response.EnsureSuccessStatusCode();
            var codeAssistProfileAsJson = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(codeAssistProfileAsJson);
            var currentTierId = (string)json["currentTier"]?["id"];
            var cloudaicompanionProjectId = (string)json["cloudaicompanionProject"];

            if (string.IsNullOrEmpty(currentTierId) || string.IsNullOrEmpty(cloudaicompanionProjectId))
            {
                throw new InvalidOperationException("Cannot retrieve your Cloud AI Companion project ID or current tier ID. Please try again with another Google account.");
            }

            if (currentTierId.Equals("legacy-tier", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Your current tier is Legacy, which is not supported. Please upgrade your tier to continue or use another Google account.");
            }

            CurrentTierId = currentTierId;
            _googleCloudProjectId = cloudaicompanionProjectId;
        }

        public async Task OnboardUserAsync()
        {
            if (string.IsNullOrEmpty(_googleCloudProjectId) || string.IsNullOrEmpty(CurrentTierId))
            {
                throw new InvalidOperationException("You must load the Code Assist profile before onboarding a free user.");
            }

            var payload = new
            {
                cloudaicompanionProject = _googleCloudProjectId,
                tierId = CurrentTierId,
            };

            await CallGeminiApiAsync("onboardUser", payload);
        }

        public async Task DisableFreeTierDataCollection()
        {
            var payload = new
            {
                cloudaicompanionProject = _googleCloudProjectId,
                freeTierDataCollectionOptin = false,
            };

            await CallGeminiApiAsync("setCodeAssistGlobalUserSetting", payload);
        }

        public async Task<ModelResponse> GenerateContentAsync(ApiRequest request, string modelAlias)
        {
            using var client = new HttpClient();
            var accessToken = await GetAccessTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GeminiCLI/v22.17.0 (win32; x64)");

            var payload = new
            {
                model = modelAlias,
                project = _googleCloudProjectId,
                request
            };

            var body = new StringContent(payload.AsString(), Encoding.UTF8, "application/json");
            HttpResponseMessage responseMessage = await client.PostAsync($"{API_ENDPOINT_PREFIX}:generateContent", body);
            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(responseContent);

            if (!responseMessage.IsSuccessStatusCode)
            {
                try
                {
                    var modelFailedResponse = jObject["error"]?.ToObject<GeminiDotNET.ApiModels.Response.Failed.ApiResponse>();

                    throw new GeminiException(modelFailedResponse == null
                            ? "Undefined"
                            : $"{modelFailedResponse.Error.Status} ({modelFailedResponse.Error.Code}): {modelFailedResponse.Error.Message}",
                        modelFailedResponse,
                        new Exception(responseContent));
                }
                catch (Exception ex)
                {
                    var dto = new GeminiDotNET.ApiModels.Response.Failed.ApiResponse
                    {
                        Error = new GeminiDotNET.ApiModels.Response.Failed.Error
                        {
                            Code = (int)responseMessage.StatusCode,
                            Message = $"{ex.Message}\n{ex.StackTrace}",
                        }
                    };

                    throw new GeminiException(dto == null
                            ? "Undefined"
                            : $"{dto.Error.Status} ({dto.Error.Code}): {dto.Error.Message}",
                        dto,
                        new Exception(responseContent));
                }
            }

            var modelSuccessResponse = jObject["response"]?.ToObject<GeminiDotNET.ApiModels.Response.Success.ApiResponse>();

            var firstCandidate = modelSuccessResponse?.Candidates.FirstOrDefault();
            var parts = firstCandidate?.Content?.Parts;
            var groudingMetadata = firstCandidate?.GroundingMetadata;

            var texts = modelSuccessResponse?.Candidates
                .Select(c => c.Content)
                .SelectMany(c => c.Parts)
                .Select(p => p.Text)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            var modelResponse = new ModelResponse
            {
                Content = string.Join("\n\n", texts),
                GroundingDetail = groudingMetadata != null
                    ? new GroundingDetail
                    {
                        RenderedContentAsHtml = groudingMetadata?.SearchEntryPoint?.RenderedContent ?? null,
                        SearchSuggestions = groudingMetadata?.WebSearchQueries,
                        ReliableInformation = groudingMetadata?.GroundingSupports?
                            .OrderByDescending(s => s.ConfidenceScores.Max())
                            .Select(s => s.Segment.Text),
                        Sources = groudingMetadata?.GroundingChunks?
                            .Select(c => new GroundingSource
                            {
                                Domain = c.Web.Title,
                                Url = c.Web.Uri,
                            }),
                    }
                    : null,

                FunctionCalls = parts != null && parts.Any(p => p.FunctionCall != null)
                    ? [.. parts.Where(p => p.FunctionCall != null).Select(p => p.FunctionCall!)]
                    : null,
                FunctionResponses = parts != null && parts.Any(p => p.FunctionResponse != null)
                    ? [.. parts.Where(p => p.FunctionResponse != null).Select(p => p.FunctionResponse!)]
                    : null,
            };

            if (modelSuccessResponse != null && modelSuccessResponse.Candidates.Count > 0)
            {
                //SetChatHistory([.. modelSuccessResponse.Candidates.Select(c => c.Content)]);
            }

            return modelResponse;
        }

        #region Helpers
        private async Task CallGeminiApiAsync(string methodName, object? payload = null)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userCredential!.Token.AccessToken);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GeminiCLI/v22.17.0 (win32; x64)");

            HttpResponseMessage? response;

            if (payload != null)
            {
                var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                response = await client.PostAsync($"{API_ENDPOINT_PREFIX}:{methodName}", body);
            }
            else
            {
                response = await client.GetAsync($"{API_ENDPOINT_PREFIX}:{methodName}");
            }

            response.EnsureSuccessStatusCode();
        }

        private static async Task<UserCredential> AuthenticateWithWebAsync()
        {
            var port = new Random().Next(1000, 20000);
            var redirectUri = $"http://localhost:{port}/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);
            listener.Start();

            string state = Guid.NewGuid().ToString("N");
            var flow = AuthenCodeFlow;
            var authorizationUrl = BuildAuthorizationUrl(AuthenCodeFlow, redirectUri, state, true);

            Process.Start(new ProcessStartInfo
            {
                FileName = authorizationUrl,
                UseShellExecute = true
            });

            string? code = null;
            string? receivedState = null;
            bool success = false;
            while (true)
            {
                var context = await listener.GetContextAsync();
                var query = context.Request.QueryString;
                code = query["code"];
                receivedState = query["state"];
                success = !string.IsNullOrEmpty(code) && receivedState == state;
                await RedirectResponseAsync(context.Response, success);
                if (success) break;
            }
            listener.Stop();

            if (string.IsNullOrEmpty(code)) throw new InvalidOperationException("No code received from OAuth flow.");

            var token = await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, CancellationToken.None);
            var credential = new UserCredential(flow, "user", token);

            Directory.CreateDirectory(GeminiDir);
            await File.WriteAllTextAsync(CredentialPath, JsonSerializer.Serialize(token).AesEncrypt());

            return credential;
        }

        private static async Task RedirectResponseAsync(HttpListenerResponse response, bool success)
        {
            string redirectUrl = success
                ? "https://developers.google.com/gemini-code-assist/auth_success_gemini"
                : "https://developers.google.com/gemini-code-assist/auth_failure_gemini";
            response.StatusCode = 302;
            response.RedirectLocation = redirectUrl;
            response.ContentType = "text/html";
            string html = $"<html><head><meta http-equiv='refresh' content='0;url={redirectUrl}'></head><body>Redirecting...</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();
        }

        private static string BuildAuthorizationUrl(GoogleAuthorizationCodeFlow flow, string redirectUri, string? state, bool selectAccount)
        {
            var urlObj = flow.CreateAuthorizationCodeRequest(redirectUri);
            var url = urlObj.Build().AbsoluteUri;
            var sb = new StringBuilder(url);
            if (state != null) sb.Append((url.Contains('?') ? "&" : "?") + $"state={state}");
            if (selectAccount && !url.Contains("prompt=")) sb.Append("&prompt=select_account");
            return sb.ToString();
        }
        #endregion
    }

}

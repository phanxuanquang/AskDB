using AskDB.Commons.Extensions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AskDB.Commons.Helpers
{
    public class GoogleOAuthenticator
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

        private string _accessToken;
        private DateTime _accessTokenExpirationTime = DateTime.Now;
        public string GoogleCloudProjectId { get; private set; }
        public string CurrentTierId { get; private set; }

        public async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_accessToken) || _accessTokenExpirationTime < DateTime.Now)
            {
                await StartAuthenticationAsync();
            }

            return _accessToken;
        }

        public async Task StartAuthenticationAsync()
        {
            if (File.Exists(CredentialPath))
            {
                var dto = await File.ReadAllTextAsync(CredentialPath);
                var token = JsonSerializer.Deserialize<TokenResponse>(dto.AesDecrypt());
                if (token == null)
                {
                    var creds = await AuthenticateWithWebAsync();
                    _accessToken = creds.Token.AccessToken;
                    return;
                }

                var userCredential = new UserCredential(AuthenCodeFlow, "user", token);
                if (await userCredential.RefreshTokenAsync(CancellationToken.None))
                {
                    _accessToken = userCredential.Token.AccessToken;
                    _accessTokenExpirationTime = userCredential.Token.ExpiresInSeconds.HasValue
                        ? DateTime.Now.AddSeconds(userCredential.Token.ExpiresInSeconds.Value).AddMinutes(-5)
                        : DateTime.MaxValue;
                    return;
                }
            }

            var authenCreds = await AuthenticateWithWebAsync();
            _accessToken = authenCreds.Token.AccessToken;
            _accessTokenExpirationTime = authenCreds.Token.ExpiresInSeconds.HasValue
                ? DateTime.Now.AddSeconds(authenCreds.Token.ExpiresInSeconds.Value).AddMinutes(-5)
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
            if (string.IsNullOrEmpty(_accessToken))
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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

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
            GoogleCloudProjectId = cloudaicompanionProjectId;
        }

        public async Task OnboardUserAsync()
        {
            if (string.IsNullOrEmpty(GoogleCloudProjectId) || string.IsNullOrEmpty(CurrentTierId))
            {
                throw new InvalidOperationException("You must load the Code Assist profile before onboarding a free user.");
            }

            var payload = new
            {
                cloudaicompanionProject = GoogleCloudProjectId,
                tierId = CurrentTierId,
            };

            await CallGeminiApiAsync("onboardUser", _accessToken, payload);
        }

        public async Task DisableFreeTierDataCollection()
        {
            var payload = new
            {
                cloudaicompanionProject = GoogleCloudProjectId,
                freeTierDataCollectionOptin = false,
            };

            await CallGeminiApiAsync("setCodeAssistGlobalUserSetting", _accessToken, payload);
        }

        #region Helpers
        private static async Task CallGeminiApiAsync(string methodName, string accessToken, object? payload = null)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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

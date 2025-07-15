using AskDB.Commons.Extensions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AskDB.Commons.Helpers
{
    public static class GoogleOAuthHelper
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

        public static async Task<string> GetAccessTokenAsync()
        {
            if (File.Exists(CredentialPath))
            {
                try
                {
                    var dto = await File.ReadAllTextAsync(CredentialPath);
                    var token = JsonSerializer.Deserialize<TokenResponse>(dto.AesDecrypt());
                    if (token == null)
                    {
                        var creds = await AuthenticateWithWebAsync();
                        return creds.Token.AccessToken;
                    }

                    var userCredential = new UserCredential(AuthenCodeFlow, "user", token);
                    if (await userCredential.RefreshTokenAsync(CancellationToken.None))
                    {
                        return userCredential.Token.AccessToken;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading cached credentials: {ex.Message}");
                }
            }

            var authenCreds = await AuthenticateWithWebAsync();
            return authenCreds.Token.AccessToken;
        }

        public static void ClearCachedUserCredential()
        {
            if (File.Exists(CredentialPath))
            {
                File.Delete(CredentialPath);
            }
        }

        public static async Task<string> LoadCodeAssistAsync(string accessToken)
        {
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
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task OnboardFreeUserAsync(string accessToken, string cloudaicompanionProjectId)
        {
            var payload = new
            {
                tierId = "free-tier",
                cloudaicompanionProject = cloudaicompanionProjectId,
            };

            await CallGeminiApiAsync("onboardUser", accessToken, payload);
        }

        public static async Task DisableFreeTierDataCollection(string accessToken, string cloudaicompanionProjectId)
        {
            var payload = new
            {
                cloudaicompanionProject = cloudaicompanionProjectId,
                freeTierDataCollectionOptin = false,
            };

            await CallGeminiApiAsync("setCodeAssistGlobalUserSetting", accessToken, payload);
        }

        public static async Task CallGeminiApiAsync(string methodName, string accessToken, object? payload = null)
        {
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

        #region Helpers
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

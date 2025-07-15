using AskDB.App.Helpers;
using AskDB.App.Local_Controls;
using AskDB.App.Pages;
using AskDB.Commons.Helpers;
using AskDB.Database;
using GeminiDotNET;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Page = Microsoft.UI.Xaml.Controls.Page;

namespace AskDB.App
{
    public sealed partial class GeminiConnection : Page
    {
        private string _geminiApiKey;
        private readonly AppDbContext _db;
        public GeminiConnection()
        {
            this.InitializeComponent();
            _db = App.LocalDb;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SetLoading(false);

            _geminiApiKey = Cache.ApiKey;
        }

        private void SetLoading(bool isLoading, string? message = null)
        {
            LoadingOverlay.SetLoading(message, isLoading, 72);
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
        }

        private void NavigateAfterAuthentication()
        {
            if (Cache.HasUserEverConnectedToDatabase)
            {
                this.Frame.Navigate(typeof(ExistingDatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
            else
            {
                this.Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }

        private async Task ShowInforBarAsync(string message)
        {
            ErrorInfoBar.Title = message;
            ErrorInfoBar.IsOpen = true;
            await Task.Delay(2000);
            ErrorInfoBar.IsOpen = false;
        }

        private async void AuthenWithApiKeyButton_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true, "Waiting for your authentication");

            var geminiApiKeyInput = new GeminiApiKeyInputDialogContent
            {
                ApiKey = Cache.ApiKey,
            };

            var authenWithApiKeyDialog = new ContentDialog
            {
                Title = "Authenticate with your Gemini API Key",
                Content = geminiApiKeyInput,
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                XamlRoot = App.Window.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await authenWithApiKeyDialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            if (string.IsNullOrEmpty(geminiApiKeyInput.ApiKey) || string.IsNullOrWhiteSpace(geminiApiKeyInput.ApiKey))
            {
                throw new InvalidOperationException("Please enter your Gemini API key");
            }

            _geminiApiKey = geminiApiKeyInput.ApiKey;

            try
            {
                SetLoading(true, "Validating your API key");

                if (string.IsNullOrEmpty(_geminiApiKey) || string.IsNullOrWhiteSpace(_geminiApiKey))
                {
                    throw new InvalidOperationException("Please enter your Gemini API key");
                }

                if (Cache.ApiKey == _geminiApiKey)
                {
                    throw new InvalidOperationException("You are already connected to Gemini with this API key.");
                }

                var generator = new Generator(_geminiApiKey);

                try
                {
                    var request = new ApiRequestBuilder()
                        .WithPrompt("Print out `Hello world`")
                        .DisableAllSafetySettings()
                        .WithDefaultGenerationConfig(0.2f, 4)
                        .Build();

                    await generator.GenerateContentAsync(request);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot validate your API Key: {ex.Message}. The error detail has been copied to your clipboard.", ex);
                }

                if (!string.IsNullOrEmpty(Cache.ApiKey))
                {
                    await _db.UpdateApiKeyAsync(_geminiApiKey);
                }
                else
                {
                    await _db.CreateOrUpdateApiKeyAsync(_geminiApiKey);
                }

                Cache.ApiKey = _geminiApiKey;

                NavigateAfterAuthentication();
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                ShowInforBarAsync(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void LoginWithGoogleButton_Click(object sender, RoutedEventArgs e)
        {
            SetLoading(true, "Waiting for your authentication");

            try
            {
                GoogleOAuthenticator.ClearCachedUserCredential();
                var authenticator = new GoogleOAuthenticator();
                await authenticator.StartAuthenticationAsync();

                SetLoading(true, "Loading your usage tier");
                await authenticator.LoadCodeAssistAsync();

                SetLoading(true, "Setting up your environment");
                await authenticator.OnboardUserAsync();

                if (authenticator.CurrentTierId.Equals("free-tier", StringComparison.OrdinalIgnoreCase))
                {
                    SetLoading(true, "Disabling data collection from Google");
                    await authenticator.DisableFreeTierDataCollection();
                }

                NavigateAfterAuthentication();
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                ShowInforBarAsync(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
    }
}

using AskDB.App.Helpers;
using AskDB.App.Pages;
using AskDB.Database;
using GeminiDotNET;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
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
            SetError(null);

            _geminiApiKey = Cache.ApiKey;
        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.SetLoading(null, isLoading, 72);
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
        }

        private void SetError(string? message)
        {
            ErrorLabel.Visibility = VisibilityHelper.SetVisible(!string.IsNullOrEmpty(message));
            ErrorLabel.Text = message ?? string.Empty;
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetLoading(true);
                SetError(null);

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
                        .WithDefaultGenerationConfig(0.2f, 400)
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

                if (Cache.HasUserEverConnectedToDatabase)
                {
                    this.Frame.Navigate(typeof(ExistingDatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                }
                else
                {
                    this.Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                }
            }
            catch (Exception ex)
            {
                ex.CopyToClipboard();
                SetError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
    }
}

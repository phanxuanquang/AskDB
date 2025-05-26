using AskDB.App.Helpers;
using AskDB.Database;
using GeminiDotNET;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
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
            _db = App.GetService<AppDbContext>();

            SetLoading(false);
            SetError(null);
        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.SetLoading("Validating API Key...", isLoading);
            LoadingOverlay.Visibility = VisibilityHelper.SetVisible(isLoading);
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
        }

        private void SetError(string message = null)
        {
            ErrorLabel.Visibility = VisibilityHelper.SetVisible(!string.IsNullOrEmpty(message));
            ErrorLabel.Text = message ?? string.Empty;
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetLoading(true);
                if (string.IsNullOrEmpty(_geminiApiKey) || string.IsNullOrWhiteSpace(_geminiApiKey))
                {
                    throw new InvalidOperationException("Please enter your Gemini API key");
                }

                try
                {
                    var generator = new Generator(_geminiApiKey);
                    var apiRequest = new ApiRequestBuilder()
                        .WithPrompt("Print out `Hello world`")
                        .DisableAllSafetySettings()
                        .WithDefaultGenerationConfig(0.2F, 400)
                        .Build();

                    await generator.GenerateContentAsync(apiRequest);

                    await _db.CreateUserAsync(_geminiApiKey);

                    Cache.ApiKey = _geminiApiKey;
                    this.Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                }
                catch
                {
                    throw new InvalidOperationException("Invalid or expired API key. Please try again with another API key.");
                }
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
    }
}

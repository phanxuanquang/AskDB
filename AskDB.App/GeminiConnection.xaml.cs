using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Gemini.NET;
using static System.Net.Mime.MediaTypeNames;
using Helper;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeminiConnection : Page
    {
        public GeminiConnection()
        {
            this.InitializeComponent();

            this.continueButton.Click += ContinueButton_Click;
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = geminiApiKeyBox.Text;

            if (!Validator.CanBeValidApiKey(apiKey))
            {
                await WinUiHelper.ShowDialog(this.RootGrid.XamlRoot, "Invalid or expired API Key. Please try again with another API key.");
                return;
            }

            SetLoadingState(true, "Validating API Key...");
            var isValidApiKey = await new Generator(apiKey).IsValidApiKeyAsync();
            
            if (!isValidApiKey)
            {
                await WinUiHelper.ShowDialog(this.RootGrid.XamlRoot, "Invalid or expired API Key. Please try again with another API key.");
                SetLoadingState(false, "Validating API Key...");
                return;
            }

            Cache.ApiKey = apiKey;
            SetLoadingState(false, "Validating API Key...");
            this.Frame.Navigate(typeof(DatabaseConnection), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }
        private void SetLoadingState(bool isLoading, string message)
        {
            continueButton.IsEnabled = !isLoading;
            LoadingOverlay.SetLoading(message, isLoading);
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            mainPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}

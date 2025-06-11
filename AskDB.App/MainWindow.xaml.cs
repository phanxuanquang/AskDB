using AskDB.App.Helpers;
using AskDB.App.Pages;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Octokit;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Application = Microsoft.UI.Xaml.Application;

namespace AskDB.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.SetTitleBarIcon("Images/icon.png");
            this.AppWindow.SetIcon("Images/icon.ico");
            this.AppWindow.Title = "AskDB";
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

            _ = FinishStartupAsync();
        }

        private async Task FinishStartupAsync()
        {
            LoadingIndicator.SetLoading(null, true, 72);

            int maxWaitMs = 500;
            int waited = 0;
            while (string.IsNullOrEmpty(Cache.ApiKey) && waited < maxWaitMs)
            {
                await Task.Delay(100);
                waited += 100;
            }

            LoadingIndicator.SetLoading(null, false);

            if (string.IsNullOrEmpty(Cache.ApiKey))
            {
                MainFrame.Navigate(typeof(GeminiConnection), null, new DrillInNavigationTransitionInfo());
                return;
            }

            if (Cache.HasUserEverConnectedToDatabase)
            {
                MainFrame.Navigate(typeof(ExistingDatabaseConnection), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                MainFrame.Navigate(typeof(DatabaseConnection), null, new DrillInNavigationTransitionInfo());
            }
        }

        private async void GithubProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://github.com/phanxuanquang");
            await Launcher.LaunchUriAsync(uri);
        }

        private void ColorThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            RootGrid.RequestedTheme = RootGrid.RequestedTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }

        private async void UpdateApiKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Cache.ApiKey))
            {
                await DialogHelper.ShowErrorAsync("Please enter your Gemini API key.");
                return;
            }

            if (MainFrame.SourcePageType != typeof(GeminiConnection))
            {
                MainFrame.Navigate(typeof(GeminiConnection), null, new DrillInNavigationTransitionInfo());
            }
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Cache.ApiKey))
            {
                await DialogHelper.ShowErrorAsync("Please enter your Gemini API key.");
                return;
            }

            if (MainFrame.SourcePageType == typeof(ExistingDatabaseConnection))
            {
                return;
            }

            if (Cache.HasUserEverConnectedToDatabase || MainFrame.SourcePageType == typeof(ExistingDatabaseConnection))
            {
                MainFrame.Navigate(typeof(ExistingDatabaseConnection), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                MainFrame.Navigate(typeof(DatabaseConnection), null, new DrillInNavigationTransitionInfo());
            }
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var client = new GitHubClient(new ProductHeaderValue(DateTime.Now.Ticks.ToString()));

            var latestRelease = await client.Repository.Release.GetLatest("phanxuanquang", "AskDB");

            var currentVersion = Assembly.GetExecutingAssembly().GetName()?.Version?.ToString();
            if (latestRelease != null && currentVersion != latestRelease.Name)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"The latest version ({latestRelease.Name}) was released on {latestRelease.CreatedAt.ToString("MMMM dd, yyyy")}.");
                sb.AppendLine();
                sb.AppendLine("Please check the description of the latest version as below:");
                sb.AppendLine(latestRelease.Body);
                sb.AppendLine();
                sb.AppendLine($"It is recommended to download and use the latest version for better experience.");

                var noti = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "New Version!",
                    Content = sb.ToString().Trim(),
                    PrimaryButtonText = "Download Now",
                    CloseButtonText = "Skip",
                    DefaultButton = ContentDialogButton.Primary
                };

                if (await noti.ShowAsync() == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri(latestRelease.Assets[0].BrowserDownloadUrl));
                    Application.Current.Exit();
                }
            }
        }

        private void SendFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            LicenseHelper.SendFeedbackAsyn();
        }
    }
}

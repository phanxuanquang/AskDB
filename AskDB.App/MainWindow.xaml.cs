using AskDB.App.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using Windows.System;

namespace AskDB.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.SetIcon("Assets/icon.ico");
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

            if (string.IsNullOrEmpty(Cache.ApiKey))
            {
                MainFrame.Navigate(typeof(GeminiConnection), null, new DrillInNavigationTransitionInfo());
                return;
            }

            MainFrame.Navigate(typeof(DatabaseConnection));
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
    }
}

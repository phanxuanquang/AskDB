using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AskDB.App
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.SetIcon("Assets/icon.ico");
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

            MainFrame.Navigate(typeof(GeminiConnection));
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

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {

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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AskDB.App
{
    public static class WinUiHelper
    {
        public static bool IsMainPageEntered = false;
        public static async Task<ContentDialogResult> ShowDialog(XamlRoot xamlRoot, string message, string title = "Error")
        {
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            return await dialog.ShowAsync();
        }

        public static void SetLoading(bool isLoading, Button button, LoadingControl loadingControl, StackPanel mainPanel, string loadingMessage = "")
        {
            mainPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            button.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            loadingControl.SetLoading(loadingMessage, isLoading);
        }
    }
}

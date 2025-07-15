using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AskDB.App.Helpers
{
    public static class DialogHelper
    {
        public static async Task ShowErrorAsync(string message, string title = "Error")
        {
            var errorDialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = App.Window.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            await errorDialog.ShowAsync();
        }

        public static async Task<ContentDialogResult> ShowDialogWithOptions(string title, string message, string primaryButtonText = "Try again", ContentDialogButton defaultButton = ContentDialogButton.Secondary)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = "Cancel",
                XamlRoot = App.Window.Content.XamlRoot,
                DefaultButton = defaultButton
            };

            return await dialog.ShowAsync();
        }
    }
}

using AskDB.App.Helpers;
using AskDB.App.Local_Controls.AIProviderConnections;
using AskDB.App.View_Models;
using AskDB.Commons.Extensions;
using AskDB.SemanticKernel.Enums;
using AskDB.SemanticKernel.Factories;
using AskDB.SemanticKernel.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.ObjectModel;


namespace AskDB.App.Pages
{
    public sealed partial class AiServiceConnection : Page
    {
        private readonly ObservableCollection<AiServiceConnectionItem> SupportedProviders =
        [
           AiServiceConnectionItem.CreateDefault(AiServiceProvider.Gemini, true),
           AiServiceConnectionItem.CreateDefault(AiServiceProvider.OpenAI, true),
           AiServiceConnectionItem.CreateDefault(AiServiceProvider.AzureOpenAI),
           AiServiceConnectionItem.CreateDefault(AiServiceProvider.ONNX),
           AiServiceConnectionItem.CreateDefault(AiServiceProvider.Ollama)
        ];

        public AiServiceConnection()
        {
            InitializeComponent();
        }

        private async void ItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not AiServiceConnectionItem item)
            {
                return;
            }

            if (!item.IsStandardProvider)
            {
                // TODO: Implement custom provider connection logic
                return;
            }

            var standardAIProviderConnection = new StandardAIProviderConnection();

            var contentDialog = new ContentDialog
            {
                Title = $"Connect to {item.ServiceProvider.GetFriendlyName()}",
                Content = standardAIProviderConnection,
                PrimaryButtonText = "Connect",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.Window.Content.XamlRoot
            };

            var result = await contentDialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var apiKey = standardAIProviderConnection.ApiKey;
            var modelId = standardAIProviderConnection.ModelId;

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(modelId))
            {
                await DialogHelper.ShowErrorAsync("API key and model codename cannot be empty.");
                return;
            }

            try
            {
                Cache.KernelFactory = item.ServiceProvider switch
                {
                    AiServiceProvider.Gemini => new KernelFactory().UseGoogleGeminiProvider(apiKey, modelId),
                    AiServiceProvider.OpenAI => new KernelFactory().UseOpenAiProvider(apiKey, modelId),
                    _ => throw new NotSupportedException("The specified AI service provider is not supported.")
                };

                var chatCompletionService = new AgentChatCompletionService(Cache.KernelFactory);
                await chatCompletionService.HealthCheckAsync();
                await DialogHelper.ShowDialogWithOptions("Connection Successful", $"You have successfully connected to {item.ServiceProvider.GetFriendlyName()}.", "Continue");

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
                Cache.KernelFactory = null;
                await DialogHelper.ShowErrorAsync(ex.Message);
            }
        }
    }
}

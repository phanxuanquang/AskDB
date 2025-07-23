using AskDB.App.Helpers;
using AskDB.App.Local_Controls.AIProviderConnections;
using AskDB.App.View_Models;
using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.SemanticKernel.Factories;
using AskDB.SemanticKernel.Helpers;
using AskDB.SemanticKernel.Models;
using AskDB.SemanticKernel.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using OllamaSharp.Models.Chat;
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

        private void SetLoading(bool isLoading)
        {
            MainPanel.Visibility = VisibilityHelper.SetVisible(!isLoading);
            LoadingOverlay.SetLoading(null, isLoading, 72);
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

            SetLoading(true);

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
                SetLoading(false);
                return;
            }

            try
            {
                var apiKey = standardAIProviderConnection.ApiKey;

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("API key cannot be empty.");
                }

                try
                {
                    var modelId = item.ServiceProvider.GetDefaultModel()!;
                    Cache.KernelFactory = item.ServiceProvider switch
                    {
                        AiServiceProvider.Gemini => new KernelFactory().UseGoogleGeminiProvider(apiKey, modelId),
                        AiServiceProvider.OpenAI => new KernelFactory().UseOpenAiProvider(apiKey, modelId),
                        AiServiceProvider.Mistral => new KernelFactory().UseMistralProvider(apiKey, modelId),
                        _ => throw new NotSupportedException("The specified AI service provider is not supported.")
                    };

                    var chatCompletionService = new AgentChatCompletionService(Cache.KernelFactory);

                    await chatCompletionService.HealthCheckAsync();
                }
                catch (Exception ex)
                {
                    Cache.KernelFactory = null;
                    throw new InvalidOperationException($"Failed to connect to {item.ServiceProvider.GetFriendlyName()}.", ex);
                }

                try
                {
                    Cache.StandardAiServiceProviderCredential = new StandardAiServiceProviderCredential
                    {
                        ApiKey = apiKey,
                        ServiceProvider = item.ServiceProvider,
                    };

                    await AiServiceProviderCredentialManager.SaveCredentialAsync(Cache.StandardAiServiceProviderCredential);
                }
                catch (Exception ex)
                {
                    Cache.StandardAiServiceProviderCredential = null;
                    throw new InvalidOperationException("Failed to save the AI service provider credential.", ex);
                }

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
                await DialogHelper.ShowErrorAsync($"{ex.Message}. {ex.InnerException?.Message}\nThe error detail has been copied to your clipboard.");
            }
            finally
            {
                SetLoading(false);
            }
        }
    }
}

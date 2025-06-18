using AskDB.SemanticKernel.Enums;
using Microsoft.SemanticKernel;

namespace AskDB.SemanticKernel.Factories
{
    public class KernelFactory
    {
        private readonly IKernelBuilder _kernelBuilder;
        public AiServiceProvider ServiceProvider { get; private set; }

        public KernelFactory()
        {
            _kernelBuilder = Kernel.CreateBuilder();
        }

        public KernelFactory WithPlugin(object plugin)
        {
            _kernelBuilder.Plugins.AddFromObject(plugin);
            return this;
        }

        public Kernel Build()
        {
            return _kernelBuilder.Build();
        }

        #region AI Service Providers
        public KernelFactory UseOpenAiProvider(string apiKey, string modelId)
        {
            ServiceProvider = AiServiceProvider.OpenAI;
            _kernelBuilder.AddOpenAIChatCompletion(modelId: modelId, apiKey: apiKey);
            return this;
        }

        public KernelFactory UseAzureOpenAiProvider(string deploymentName, string apiKey, string endpoint)
        {
            ServiceProvider = AiServiceProvider.AzureOpenAI;
            _kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName: deploymentName, endpoint: endpoint, apiKey: apiKey);
            return this;
        }

        public KernelFactory UseGoogleGeminiProvider(string apiKey, string modelId)
        {
            ServiceProvider = AiServiceProvider.Gemini;
#pragma warning disable SKEXP0070
            _kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId: modelId, apiKey: apiKey);
            return this;
        }
        #endregion
    }
}

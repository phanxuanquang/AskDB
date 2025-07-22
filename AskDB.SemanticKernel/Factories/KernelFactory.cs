using AskDB.SemanticKernel.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0070

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

        public KernelFactory WithService<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            _kernelBuilder.Services.AddSingleton<TInterface, TImplementation>();
            return this;
        }

        public KernelFactory WithFunctionInvocationFilter<TFilter>(TFilter implementation)
            where TFilter : class, IFunctionInvocationFilter
        {
            _kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter>(implementation);
            return this;
        }

        public KernelFactory WithAutoFunctionInvocationFilter<TFilter>(TFilter implementation)
           where TFilter : class, IAutoFunctionInvocationFilter
        {
            _kernelBuilder.Services.AddSingleton<IAutoFunctionInvocationFilter>(implementation);
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

        public KernelFactory UseAzureOpenAiProvider(string apiKey, string endpoint, string deploymentName)
        {
            ServiceProvider = AiServiceProvider.AzureOpenAI;
            _kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName: deploymentName, endpoint: endpoint, apiKey: apiKey);
            return this;
        }

        public KernelFactory UseGoogleGeminiProvider(string apiKey, string modelId)
        {
            ServiceProvider = AiServiceProvider.Gemini;
            _kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId: modelId, apiKey: apiKey);
            return this;
        }
        #endregion
    }
}

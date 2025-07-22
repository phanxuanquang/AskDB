using AskDB.SemanticKernel.Enums;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0070

namespace AskDB.SemanticKernel.Factories
{
    public static class PromptExecutionSettingsFactory
    {
        public static PromptExecutionSettings CreatePromptExecutionSettingsWithFunctionCalling(this AiServiceProvider serviceProvider, int maxOutputToken = 2048, double temperature = 1)
        {
            var promptExecutionSettings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                    options: new FunctionChoiceBehaviorOptions
                    {
                        AllowConcurrentInvocation = false,
                        AllowParallelCalls = false,
                    },
                    autoInvoke: true)
            };

            promptExecutionSettings = serviceProvider switch
            {
                AiServiceProvider.OpenAI => new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    FunctionChoiceBehavior = promptExecutionSettings.FunctionChoiceBehavior,
                    MaxTokens = maxOutputToken,
                    Temperature = temperature
                },
                AiServiceProvider.Gemini => new GeminiPromptExecutionSettings
                {
                    ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
                    FunctionChoiceBehavior = promptExecutionSettings.FunctionChoiceBehavior,
                    MaxTokens = maxOutputToken,
                    Temperature = temperature
                },
                AiServiceProvider.AzureOpenAI => AzureOpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings, maxOutputToken),
                AiServiceProvider.Ollama => OllamaPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings),
                AiServiceProvider.ONNX => promptExecutionSettings as OnnxRuntimeGenAIPromptExecutionSettings,
                _ => throw new NotImplementedException(),
            };

            return promptExecutionSettings!;
        }

        public static PromptExecutionSettings CreatePromptExecutionSettings(this AiServiceProvider serviceProvider, int maxOutputToken = 2048, double temperature = 1)
        {
            var promptExecutionSettings = new PromptExecutionSettings();

            promptExecutionSettings = serviceProvider switch
            {
                AiServiceProvider.OpenAI => new OpenAIPromptExecutionSettings
                {
                    MaxTokens = maxOutputToken,
                    Temperature = temperature
                },
                AiServiceProvider.Gemini => new GeminiPromptExecutionSettings
                {
                    MaxTokens = maxOutputToken,
                    Temperature = temperature
                },
                AiServiceProvider.AzureOpenAI => AzureOpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings, maxOutputToken),
                AiServiceProvider.Ollama => OllamaPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings),
                AiServiceProvider.ONNX => promptExecutionSettings as OnnxRuntimeGenAIPromptExecutionSettings,
                _ => throw new NotImplementedException(),
            };

            return promptExecutionSettings!;
        }

        public static PromptExecutionSettings CreatePromptExecutionSettingsForJsonOutput<T>(this PromptExecutionSettings promptExecutionSettings, AiServiceProvider serviceProvider)
        {
            switch (serviceProvider)
            {
                case AiServiceProvider.OpenAI:
                    OpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseFormat = typeof(T);
                    break;
                case AiServiceProvider.Gemini:
                    GeminiPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseSchema = typeof(T);
                    GeminiPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseMimeType = "application/json";
                    break;
                case AiServiceProvider.AzureOpenAI:
                    AzureOpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseFormat = typeof(T);
                    break;
                default: throw new NotImplementedException($"The service provider {serviceProvider} does not support JSON output format.");
            }

            return promptExecutionSettings;
        }
    }
}

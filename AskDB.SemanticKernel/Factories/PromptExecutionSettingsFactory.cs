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
        public static PromptExecutionSettings CreatePromptExecutionSettings(this AiServiceProvider serviceProvider)
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
                },
                AiServiceProvider.Gemini => new GeminiPromptExecutionSettings
                {
                    ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
                    FunctionChoiceBehavior = promptExecutionSettings.FunctionChoiceBehavior,
                },
                AiServiceProvider.AzureOpenAI => AzureOpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings),
                AiServiceProvider.Ollama => OllamaPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings),
                AiServiceProvider.ONNX => promptExecutionSettings as OnnxRuntimeGenAIPromptExecutionSettings,
                _ => throw new NotImplementedException(),
            };

            return promptExecutionSettings!;
        }

        public static PromptExecutionSettings CreatePromptExecutionSettingsForJsonOutput(this PromptExecutionSettings promptExecutionSettings, AiServiceProvider serviceProvider, object jsonFormat)
        {
            switch (serviceProvider)
            {
                case AiServiceProvider.OpenAI:
                    OpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseFormat = jsonFormat;
                    break;
                case AiServiceProvider.Gemini:
                    GeminiPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseSchema = jsonFormat;
                    GeminiPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseMimeType = "application/json";
                    break;
                case AiServiceProvider.AzureOpenAI:
                    AzureOpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings).ResponseFormat = jsonFormat;
                    break;
                default: throw new NotImplementedException($"The service provider {serviceProvider} does not support JSON output format.");
            }

            return promptExecutionSettings;
        }
    }
}

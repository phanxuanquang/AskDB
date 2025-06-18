using AskDB.SemanticKernel.Enums;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AskDB.SemanticKernel.Factories
{
    public static class PromptExecutionSettingsFactory
    {
        public static PromptExecutionSettings CreatePromptExecutionSettings(this AiServiceProvider serviceProvider)
        {
            PromptExecutionSettings promptExecutionSettings;

#pragma warning disable SKEXP0070
            promptExecutionSettings = serviceProvider switch
            {
                AiServiceProvider.OpenAI => new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                },
                AiServiceProvider.Gemini => new GeminiPromptExecutionSettings
                {
                    ToolCallBehavior = GeminiToolCallBehavior.EnableKernelFunctions,
                },
                AiServiceProvider.AzureOpenAI => new AzureOpenAIPromptExecutionSettings(),
                AiServiceProvider.ONNX => new OnnxRuntimeGenAIPromptExecutionSettings(),
                AiServiceProvider.Ollama => new OllamaPromptExecutionSettings(),
                _ => throw new NotImplementedException(),
            };

            promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

            return promptExecutionSettings;
        }
    }
}

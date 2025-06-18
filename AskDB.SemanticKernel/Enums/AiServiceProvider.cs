using System.ComponentModel;

namespace AskDB.SemanticKernel.Enums
{
    public enum AiServiceProvider
    {
        [Description("OpenAI")]
        OpenAI,

        [Description("Azure OpenAI")]
        AzureOpenAI,

        [Description("Gemini")]
        Gemini,

        [Description("ONNX")]
        ONNX,

        [Description("Ollma")]
        Ollama,
    }
}

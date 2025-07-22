using AskDB.Commons.Attributes;

namespace AskDB.SemanticKernel.Enums
{
    public enum AiServiceProvider
    {
        [FriendlyName("OpenAI")]
        OpenAI,

        [FriendlyName("Azure OpenAI")]
        AzureOpenAI,

        [FriendlyName("Gemini")]
        Gemini,

        [FriendlyName("ONNX")]
        ONNX,

        [FriendlyName("Ollama")]
        Ollama,
    }
}

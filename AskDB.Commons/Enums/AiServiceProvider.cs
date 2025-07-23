using AskDB.Commons.Attributes;

namespace AskDB.Commons.Enums
{
    public enum AiServiceProvider
    {
        [DefaultModel("gpt-3.5-turbo")]
        [FriendlyName("OpenAI")]
        OpenAI,

        [DefaultModel("gpt-35-turbo")]
        [FriendlyName("Azure OpenAI")]
        AzureOpenAI,

        [DefaultModel("gemini-2.5-flash")]
        [FriendlyName("Gemini")]
        Gemini,

        [FriendlyName("ONNX")]
        ONNX,

        [FriendlyName("Ollama")]
        Ollama,

        [FriendlyName("Mistral")]
        Mistral
    }
}

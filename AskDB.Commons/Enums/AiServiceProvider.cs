using AskDB.Commons.Attributes;

namespace AskDB.Commons.Enums
{
    public enum AiServiceProvider
    {
        [FriendlyName("OpenAI")]
        [DefaultModel("gpt-3.5-turbo")]
        OpenAI,

        [FriendlyName("Azure OpenAI")]
        [DefaultModel("gpt-35-turbo")]
        AzureOpenAI,

        [FriendlyName("Google Gemini")]
        [DefaultModel("gemini-2.5-flash")]
        Gemini,

        [FriendlyName("ONNX")]
        ONNX,

        [FriendlyName("Ollama")]
        Ollama,

        [FriendlyName("Mistral")]
        Mistral
    }
}

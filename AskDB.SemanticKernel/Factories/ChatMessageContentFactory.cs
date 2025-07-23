using AskDB.Commons.Enums;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0070
namespace AskDB.SemanticKernel.Factories
{
    public static class ChatMessageContentFactory
    {
        public static ChatMessageContent CreateChatMessageContent(this ChatMessageContent chatMessageContent, AiServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(chatMessageContent);

            return serviceProvider switch
            {
                AiServiceProvider.OpenAI => chatMessageContent as OpenAIChatMessageContent,
                AiServiceProvider.Gemini => chatMessageContent as GeminiChatMessageContent,
                _ => chatMessageContent,
            };
        }

    }
}

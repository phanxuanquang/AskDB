using AskDB.SemanticKernel.Enums;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AskDB.SemanticKernel.Factories
{
    public static class ChatMessageContentFactory
    {
        public static ChatMessageContent CreateChatMessageContent(this ChatMessageContent chatMessageContent, AiServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(chatMessageContent);

#pragma warning disable SKEXP0070
            return serviceProvider switch
            {
                AiServiceProvider.OpenAI => chatMessageContent as OpenAIChatMessageContent,
                AiServiceProvider.Gemini => chatMessageContent as GeminiChatMessageContent,
                _ => chatMessageContent,
            };
        }

    }
}

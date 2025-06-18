using AskDB.SemanticKernel.Enums;
using AskDB.SemanticKernel.Factories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AskDB.SemanticKernel.Services
{
    public class ChatCompletionService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly AiServiceProvider _serviceProvider;
        private readonly int _maxMessageCount;

        private ChatHistory _chatHistory;

        public ChatCompletionService(KernelFactory kernelFactory, int maxMessageCount = 200)
        {
            _kernel = kernelFactory.Build();
            _serviceProvider = kernelFactory.ServiceProvider;
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            _chatHistory = [];
            _maxMessageCount = maxMessageCount;
        }

        public ChatCompletionService WithSystemInstruction(string systemInstruction)
        {
            if (string.IsNullOrWhiteSpace(systemInstruction))
            {
                throw new ArgumentException("System instruction cannot be null or empty.", nameof(systemInstruction));
            }

            _chatHistory.Clear();

            _chatHistory.Add(
                new()
                {
                    Role = AuthorRole.System,
                    Content = systemInstruction
                }
            );

            return this;
        }

        public async Task<ChatMessageContent> SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            _chatHistory.AddUserMessage(message.Trim());

            if (_chatHistory.Count > _maxMessageCount)
            {
                var reducer = new ChatHistoryTruncationReducer(targetCount: _maxMessageCount);
                var reducedMessages = await reducer.ReduceAsync(_chatHistory);
                if (reducedMessages is not null)
                {
                    _chatHistory = [.. reducedMessages];
                }
            }
#pragma warning disable SKEXP0070

            var chatCompletion = await _chatCompletionService.GetChatMessageContentsAsync(
                _chatHistory,
                executionSettings: _serviceProvider.CreatePromptExecutionSettings(),
                kernel: _kernel);

            return chatCompletion[0];
        }
    }
}

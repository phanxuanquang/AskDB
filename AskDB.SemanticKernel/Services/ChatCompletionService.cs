using AskDB.SemanticKernel.Factories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AskDB.SemanticKernel.Services
{
    public class ChatCompletionService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly int _maxMessageCount;
        private readonly PromptExecutionSettings _promptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        private ChatHistory _chatHistory;

        public ChatCompletionService(KernelFactory kernelFactory, int maxMessageCount = 200)
        {
            _kernel = kernelFactory.Build();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            _chatHistory = [];
            _maxMessageCount = maxMessageCount;
        }

        public void AddFunctionCallingResponse(FunctionResultContent functionResultContent)
        {
            _chatHistory.Add(functionResultContent.ToChatMessage());
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

            var chatCompletion = await _chatCompletionService.GetChatMessageContentAsync(
                _chatHistory,
                executionSettings: _promptExecutionSettings,
                kernel: _kernel);

            _chatHistory.Add(chatCompletion);

            return chatCompletion;
        }
    }
}

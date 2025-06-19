using AskDB.SemanticKernel.Enums;
using AskDB.SemanticKernel.Factories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AskDB.SemanticKernel.Services
{
    public class AgentChatCompletionService
    {
        private readonly Kernel _kernel;
        public readonly AiServiceProvider ServiceProvider;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly int _maxMessageCount;

        private ChatHistory _chatHistories { get; set; }

        public AgentChatCompletionService(KernelFactory kernelFactory, int maxMessageCount = 200)
        {
            _kernel = kernelFactory.Build();
            ServiceProvider = kernelFactory.ServiceProvider;
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            _chatHistories = [];
            _maxMessageCount = maxMessageCount;
        }

        public void AddChatHistory(ChatMessageContent chatMessageContent)
        {
            _chatHistories.Add(chatMessageContent);
        }

        public void AddFunctionCallingResponse(FunctionResultContent functionResultContent)
        {
            _chatHistories.Add(functionResultContent.ToChatMessage());
        }

        public AgentChatCompletionService WithSystemInstruction(string systemInstruction)
        {
            if (string.IsNullOrWhiteSpace(systemInstruction))
            {
                throw new ArgumentException("System instruction cannot be null or empty.", nameof(systemInstruction));
            }

            _chatHistories.Clear();

            _chatHistories.Add(
                new()
                {
                    Role = AuthorRole.System,
                    Content = systemInstruction
                }
            );

            return this;
        }

        public async Task<ChatMessageContent> SendMessageAsync(string message, PromptExecutionSettings executionSettings)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            _chatHistories.AddUserMessage(message.Trim());

            if (_chatHistories.Count > _maxMessageCount)
            {
                var reducer = new ChatHistoryTruncationReducer(targetCount: _maxMessageCount);
                var reducedMessages = await reducer.ReduceAsync(_chatHistories);
                if (reducedMessages is not null)
                {
                    _chatHistories = [.. reducedMessages];
                }
            }

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                _chatHistories,
                executionSettings: executionSettings,
                kernel: _kernel);

            _chatHistories.Add(response);

            return response;
        }
    }
}

using AskDB.SemanticKernel.Enums;
using AskDB.SemanticKernel.Factories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace AskDB.SemanticKernel.Services
{
    public class AgentChatCompletionService
    {
        public readonly Kernel Kernel;
        public readonly AiServiceProvider ServiceProvider;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly int _maxMessageCount;

        private ChatHistory _chatHistories;

        public AgentChatCompletionService(KernelFactory kernelFactory, int maxMessageCount = 200)
        {
            Kernel = kernelFactory.Build();
            ServiceProvider = kernelFactory.ServiceProvider;
            _chatCompletionService = Kernel.GetRequiredService<IChatCompletionService>();
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

        public async Task<ChatMessageContent> SendMessageAsync(string message, double temperature = 1, int maxOutputToken = 2048)
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
                executionSettings: ServiceProvider.CreatePromptExecutionSettingsWithFunctionCalling(maxOutputToken, temperature),
                kernel: Kernel);

            _chatHistories.Add(response);

            return response;
        }

        public async Task<T> SendMessageAsync<T>(string message, double temperature = 1, int maxOutputToken = 2048)
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
                executionSettings: ServiceProvider.CreatePromptExecutionSettings(maxOutputToken, temperature).CreatePromptExecutionSettingsForJsonOutput<T>(ServiceProvider),
                kernel: Kernel);

            _chatHistories.Add(response);

            return JsonSerializer.Deserialize<T>(response.ToString());
        }

        public async Task HealthCheckAsync()
        {
            if (_chatCompletionService is null)
            {
                throw new InvalidOperationException("Chat completion service is not initialized.");
            }
            try
            {
                await _chatCompletionService.GetChatMessageContentAsync(
                    [
                        new ChatMessageContent
                        {
                            Role = AuthorRole.User,
                            Content = "Healthcheck message, please say `Hello World`"
                        }
                    ],
                    executionSettings: ServiceProvider.CreatePromptExecutionSettingsWithFunctionCalling(5, 0.2),
                    kernel: Kernel);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message, ex.InnerException);
            }
        }
    }
}

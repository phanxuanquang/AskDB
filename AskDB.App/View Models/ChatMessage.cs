using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Data;

namespace AskDB.App.View_Models
{
    public class ChatMessage
    {
        public string? Message { get; set; } = null;
        public AuthorRole Role { get; set; } 
        public DataTable? Data { get; set; } = null;

        public static ChatMessage CreateUserMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            return new ChatMessage
            {
                Message = message.Trim(),
                Role = AuthorRole.User
            };
        }

        public static ChatMessage CreateAssistantMessage(string? message, DataTable? data = null)
        {
            return new ChatMessage
            {
                Message = message,
                Role = AuthorRole.Assistant,
                Data = data
            };
        }
    }
}

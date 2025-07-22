using AskDB.SemanticKernel.Enums;
using System.Collections.Generic;

namespace AskDB.App.View_Models
{
    public class AiServiceConnectionItem
    {
        public required AiServiceProvider ServiceProvider { get; set; }
        public bool IsStandardProvider { get; set; } = false;
        public List<string> AvailableModels { get; set; } = [];

        public static AiServiceConnectionItem CreateDefault(AiServiceProvider serviceProvider, bool isStandardProvider = false)
        {
            return new AiServiceConnectionItem
            {
                ServiceProvider = serviceProvider,
                IsStandardProvider = isStandardProvider,
                AvailableModels = []
            };
        }
    }
}

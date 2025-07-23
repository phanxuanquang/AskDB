using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace AskDB.App.View_Models
{
    public class AiServiceConnectionItem
    {
        public required AiServiceProvider ServiceProvider { get; set; }
        public bool IsStandardProvider { get; set; } = false;
        public List<string> AvailableModels { get; set; } = [];
        public string? LogoFilePath { get; set; } 

        public static AiServiceConnectionItem CreateDefault(AiServiceProvider serviceProvider, bool isStandardProvider = false)
        {
            return new AiServiceConnectionItem
            {
                ServiceProvider = serviceProvider,
                IsStandardProvider = isStandardProvider,
                AvailableModels = [],
                LogoFilePath = Path.Combine(AppContext.BaseDirectory, "Images", "AI Service Provider Logos", $"{serviceProvider.GetFriendlyName()}.png")
            };
        }
    }
}

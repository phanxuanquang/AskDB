using AskDB.Commons.Enums;

namespace AskDB.SemanticKernel.Models
{
    public class StandardAiServiceProviderCredential
    {
        public required AiServiceProvider ServiceProvider { get; set; }
        public required string ApiKey { get; set; }
    }
}

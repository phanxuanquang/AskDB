using System.Collections.Generic;

namespace AskDB.App.View_Models
{
    public class AgentResponse
    {
        public string Summarization { get; set; }
        public List<string> UserResponseSuggestions { get; set; }
    }
}

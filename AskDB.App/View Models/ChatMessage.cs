using System.Data;

namespace AskDB.App.View_Models
{
    public class ChatMessage
    {
        public string? Message { get; set; }
        public bool IsFromUser { get; set; }
        public bool IsFromAgent { get; set; }
        public DataTable? Data { get; set; }
        public long? QueryResultId { get; set; }
        public DataVisualizationInfo? DataVisualizationInfo { get; set; } = null;
    }
}

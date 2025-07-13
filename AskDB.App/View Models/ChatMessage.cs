using System.Data;

namespace AskDB.App.View_Models
{
    public class ChatMessage
    {
        public string? Message { get; set; } = null;
        public bool IsFromUser { get; set; }
        public bool IsFromAgent { get; set; }
        public bool IsChart { get; set; } = false;
        public DataTable? Data { get; set; } = null;
        public long? QueryResultId { get; set; } = null;
        public DataVisualizationInfo? DataVisualizationInfo { get; set; } = null;
    }
}

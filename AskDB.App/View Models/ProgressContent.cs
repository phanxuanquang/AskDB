using Microsoft.UI.Xaml;
using System.Data;

namespace AskDB.App.View_Models
{
    public class ProgressContent
    {
        public string Message { get; set; }
        public string SqlCommand { get; set; }
        public DataTable Data { get; set; }
        public Visibility ActionButtonVisibility { get; set; }
    }
}

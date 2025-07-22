﻿using System.Data;

namespace AskDB.App.View_Models
{
    public class ChatMessage
    {
        public string? Message { get; set; } = null;
        public bool IsFromUser { get; set; }
        public bool IsFromAgent { get; set; }
        public DataTable? Data { get; set; } = null;
    }
}

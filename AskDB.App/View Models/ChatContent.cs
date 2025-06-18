﻿using System.Collections.ObjectModel;
using System.Data;

namespace AskDB.App.View_Models
{
    public class ChatContent
    {
        public string Message { get; set; }
        public bool IsFromUser { get; set; }
        public bool IsFromAgent { get; set; }
        public DataTable? Data { get; set; }
        public ObservableCollection<object> QueryResults { get; set; } = [];
    }
}

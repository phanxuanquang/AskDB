using AskDB.Commons.Enums;
using System;

namespace AskDB.App.View_Models
{
    public class ExistingConnectionStringInfor
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public string DatabaseTypeDisplayName { get; set; }
        public DateTime LastAccess { get; set; }
    }
}

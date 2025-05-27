using AskDB.Commons.Enums;
using System;

namespace AskDB.App.View_Models
{
    public class ExistingDatabaseConnectionInfo
    {
        public Guid Id { get; set; }
        public string Host { get; set; }
        public string Database { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public string DatabaseTypeDisplayName { get; set; }
        public DateTime LastAccess { get; set; }
        public string ConnectionString { get; set; }
    }
}

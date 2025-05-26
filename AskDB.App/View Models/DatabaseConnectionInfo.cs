using AskDB.Commons.Enums;

namespace AskDB.App.ViewModels
{
    public class DatabaseConnectionInfo
    {
        public string ConnectionString { get; set; }
        public DatabaseType DatabaseType { get; set; }
    }
}

using DatabaseInteractor.Models.Enums;

namespace AskDB.App.View_Models
{
    public class DatabaseConnectionInfo
    {
        public string ConnectionString { get; set; }
        public DatabaseType DatabaseType { get; set; }
    }
}

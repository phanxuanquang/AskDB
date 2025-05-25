using DatabaseInteractor.Models.Enums;

namespace AskDB.App.ViewModels
{
    public class DatabaseConnectionCredential
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
    }
}

using System.ComponentModel;

namespace DatabaseInteractor.Models.Enums
{
    public enum DatabaseType
    {
        [Description("Integrated Security=True;Server=localhost;Database=YourDatabaseName")]
        SqlServer = 0,

        [Description("Host=localhost;Username=postgres;Password=YourPassword;Database=YourDatabaseName")]
        PostgreSQL = 1,

        [Description("SslMode=Preferred;Server=localhost;User=root;Password=;Database=YourDatabaseName;")]
        MySQL = 2,

        [Description("Data Source=path\\to\\YourDatabaseName.db")]
        SQLite = 3,
    }
}

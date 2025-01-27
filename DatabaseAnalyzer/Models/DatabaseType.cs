using System.ComponentModel;

namespace DatabaseAnalyzer.Models
{
    public enum DatabaseType
    {
        [Description("Integrated Security=True;Server=localhost;Database=YourDatabaseName")]
        SqlServer = 1,

        [Description("Host=localhost;Username=postgres;Password=YourPassword;Database=YourDatabaseName")]
        PostgreSQL = 2,

        [Description("SslMode=Preferred;Server=localhost;User=root;Password=;Database=YourDatabaseName;")]
        MySQL = 3,

        [Description("Data Source=path\\to\\YourDatabaseName.db")]
        SQLite = 4,
    }
}

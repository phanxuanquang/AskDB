using System.ComponentModel;

namespace DatabaseAnalyzer.Models
{
    public enum DatabaseType
    {
        [Description("Integrated Security=True;Server=localhost;Database=YourDatabaseName")]
        SqlServer = 0,

        [Description("Host=localhost;Username=postgres;Password=YourPassword;Database=YourDatabaseName")]
        PostgreSQL = 1,

        [Description("MySQL")]
        MySQL = 2,

        [Description("Data Source=path\\to\\your\\database.db")]
        SQLite = 3,
    }
}

using System.ComponentModel;

namespace DatabaseAnalyzer.Models
{
    public enum DatabaseType
    {
        [Description("SQL Server")]
        MSSQL = 0,

        [Description("PostgreSQL")]
        PostgreSQL = 1,

        [Description("MySQL")]
        MySQL = 2,

        [Description("MariaDB")]
        MariaDB = 3,

        [Description("SQLite")]
        SQLite = 4,
    }
}

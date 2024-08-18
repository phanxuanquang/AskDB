using System.ComponentModel;

namespace DatabaseAnalyzer.Models
{
    public enum DatabaseType
    {
        [Description("SQL Server")]
        MSSQL,

        [Description("PostgreSQL")]
        PostgreSQL,

        [Description("MySQL")]
        MySQL,

        [Description("MariaDB")]
        MariaDB,

        [Description("SQLite")]
        SQLite,
    }
}

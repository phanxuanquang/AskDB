using System.ComponentModel;

namespace DatabaseInteractor.Models.Enums
{
    public enum DatabaseType
    {
        [Description("SQL Server")]
        SqlServer,

        [Description("PostgreSQL")]
        PostgreSQL,

        [Description("MySQL")]
        MySQL,

        [Description("SQLite")]
        SQLite
    }
}

using AskDB.Commons.Attributes;
using System.ComponentModel;

namespace AskDB.Commons.Enums
{
    public enum DatabaseType
    {
        [Description("SQL Server"), DefaultPort(1433), DefaultHost("localhost")]
        SqlServer,

        [Description("PostgreSQL"), DefaultPort(5432), DefaultHost("localhost")]
        PostgreSQL,

        [Description("MySQL"), DefaultPort(3306), DefaultHost("localhost")]
        MySQL,

        [Description("SQLite"), DefaultPort(0), DefaultHost("")]
        SQLite
    }
}

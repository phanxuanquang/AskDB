using AskDB.Commons.Attributes;
using System.ComponentModel;

namespace AskDB.Commons.Enums
{
    public enum DatabaseType
    {
        [Description("SQL Server"), DefaultPort(1433), DefaultHost("127.0.0.1")]
        SqlServer = 0,

        [Description("PostgreSQL"), DefaultPort(5432), DefaultHost("127.0.0.1")]
        PostgreSQL = 1,

        [Description("MySQL"), DefaultPort(3306), DefaultHost("127.0.0.1")]
        MySQL = 2,

        [Description("SQLite"), DefaultPort(0), DefaultHost("")]
        SQLite = 3,

        [Description("MariaDB"), DefaultPort(3306), DefaultHost("127.0.0.1")]
        MariaDB = 4
    }
}

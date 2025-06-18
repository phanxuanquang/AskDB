using AskDB.Commons.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using System.Data.Common;

namespace DatabaseInteractor.Factories
{
    public static class DbConnectionFactory
    {
        public static DbConnection CreateDbConnection(this DatabaseType dbType, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            connectionString = connectionString.Trim();

            return dbType switch
            {
                DatabaseType.SqlServer => new SqlConnection(connectionString),
                DatabaseType.MySQL => new MySqlConnection(connectionString),
                DatabaseType.MariaDB => new MySqlConnection(connectionString),
                DatabaseType.PostgreSQL => new NpgsqlConnection(connectionString),
                DatabaseType.SQLite => new SqliteConnection(connectionString),
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }
    }
}

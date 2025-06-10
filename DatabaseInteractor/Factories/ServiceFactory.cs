using AskDB.Commons.Enums;
using DatabaseInteractor.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Data.Common;

namespace DatabaseInteractor.Factories
{
    public static class ServiceFactory
    {
        public static DbConnection CreateConnection(DatabaseType dbType, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            return dbType switch
            {
                DatabaseType.SqlServer => new SqlConnection(connectionString),
                DatabaseType.MySQL => new MySqlConnection(connectionString),
                DatabaseType.PostgreSQL => new NpgsqlConnection(connectionString),
                DatabaseType.SQLite => new SqliteConnection(connectionString),
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }

        public static DatabaseInteractionService CreateInteractionService(DatabaseType dbType, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            return dbType switch
            {
                DatabaseType.SqlServer => new SqlServerService(connectionString),
                DatabaseType.MySQL => new MySqlService(connectionString),
                DatabaseType.PostgreSQL => new PostgreSqlService(connectionString),
                DatabaseType.SQLite => new SqliteService(connectionString),
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }
    }
}

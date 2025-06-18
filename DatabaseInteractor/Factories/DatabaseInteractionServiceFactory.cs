using AskDB.Commons.Enums;
using DatabaseInteractor.Services;

namespace DatabaseInteractor.Factories
{
    public static class DatabaseInteractionServiceFactory
    {
        public static DatabaseInteractionService CreateDatabaseInteractionService(this DatabaseType dbType, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            connectionString = connectionString.Trim();

            return dbType switch
            {
                DatabaseType.SqlServer => new SqlServerService(connectionString),
                DatabaseType.MySQL => new MySqlService(connectionString),
                DatabaseType.MariaDB => new MariaDbService(connectionString),
                DatabaseType.PostgreSQL => new PostgreSqlService(connectionString),
                DatabaseType.SQLite => new SqliteService(connectionString),
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }
    }
}

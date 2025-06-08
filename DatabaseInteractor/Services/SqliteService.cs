using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Microsoft.Data.Sqlite;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class SqliteService : DatabaseInteractionService
    {
        public SqliteService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SQLite;
        }

        public override async Task<int> GetTableCountAsync()
        {
            var query = "SELECT COUNT(*) FROM sqlite_master WHERE type='table';";
            await using var connection = new SqliteConnection(ConnectionString);
            await using var command = new SqliteCommand(query, connection);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrEmpty(table))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
            }
            var query = $"PRAGMA table_info({table})";
            return await ExecuteQueryAsync(query);
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            return
            [
                "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER", "INDEX", "TRIGGER"
            ];
        }

        public override async Task<List<string>> SearchTablesByNameAsync(string? keyword, int maxResult = 20000)
        {
            await using var command = new SqliteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name LIKE @keyword LIMIT {maxResult}");
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }
    }
}

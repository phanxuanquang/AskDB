using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Microsoft.Data.Sqlite;
using System.Data;

namespace DatabaseInteractor.Services.Extractors
{
    public class SqliteExtractor : ExtractorBase
    {
        public SqliteExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SQLite;
        }

        private async Task<DataTable> ExecuteQueryAsync(SqliteCommand command)
        {
            await using var connection = new SqliteConnection(ConnectionString);
            command.Connection = connection;

            await connection.OpenAsync();

            var dataTable = new DataTable();
            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            await using var command = new SqliteCommand(sqlQuery);
            return await ExecuteQueryAsync(command);
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            await using var connection = new SqliteConnection(ConnectionString);
            await using var command = new SqliteCommand(sqlQuery, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public override Task<List<string>> GetUserPermissionsAsync()
        {
            return Task.FromResult<List<string>>([
                "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER", "INDEX", "TRIGGER"
            ]);
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

        public override async Task EnsureDatabaseConnectionAsync()
        {
            await using var connection = new SqliteConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message, ex.InnerException);
            }
        }

        public override async Task<List<string>> SearchTablesByNameAsync(string? keyword)
        {
            await using var command = new SqliteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name LIKE @keyword");
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            var data = await ExecuteQueryAsync(command.CommandText);
            return data.ToListString();
        }

        public override async Task<int> GetTableCountAsync()
        {
            var query = "SELECT COUNT(*) FROM sqlite_master WHERE type='table';";
            await using var connection = new SqliteConnection(ConnectionString);
            await using var command = new SqliteCommand(query, connection);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
    }
}

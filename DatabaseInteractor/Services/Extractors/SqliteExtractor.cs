using AskDB.Commons.Enums;
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

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            using var connection = new SqliteConnection(ConnectionString);
            using var command = new SqliteCommand(sqlQuery, connection);
            var dataTable = new DataTable();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            using var connection = new SqliteConnection(ConnectionString);
            using var command = new SqliteCommand(sqlQuery, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public override Task<List<string>> GetUserPermissionsAsync()
        {
            return Task.FromResult<List<string>>([
                "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER", "INDEX", "TRIGGER"
            ]);
        }

        public override Task<List<string>> SearchSchemasByNameAsync(string? keyword)
        {
            throw new NotImplementedException();
        }

        public override Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            throw new NotImplementedException();
        }

        public override async Task EnsureDatabaseConnectionAsync()
        {
            using var connection = new SqliteConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message, ex.InnerException);
            }
        }

        public override async Task<List<string>> SearchTablesByNameAsync(string? schema, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                keyword = string.Empty;
            }
            using var connection = new SqliteConnection(ConnectionString);
            using var command = new SqliteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name LIKE @keyword", connection);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
    }
}

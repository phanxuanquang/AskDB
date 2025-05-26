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

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            return
            [
                "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER", "INDEX", "TRIGGER"
            ];
        }

        public override Task<List<string>> GetDatabaseSchemaNamesAsync(string? keyword)
        {
            throw new NotImplementedException();
        }

        public override Task<DataTable> GetSchemaInfoAsync(string table, string? schema)
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
    }
}

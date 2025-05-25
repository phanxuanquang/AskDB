using DatabaseInteractor.Models.Enums;
using Npgsql;
using System.Data;

namespace DatabaseInteractor.Services.Extractors
{
    public class PostgreSqlExtractor : ExtractorBase
    {
        public PostgreSqlExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.PostgreSQL;
        }

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            using var command = new NpgsqlCommand(sqlQuery, connection);
            var dataTable = new DataTable();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            using var command = new NpgsqlCommand(sqlQuery, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var permissions = new List<string>();
            var query = "SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = CURRENT_USER";

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                using var command = new NpgsqlCommand(query, connection);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    permissions.Add(reader.GetString(0));
                }
            }
            return permissions;
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
            using var connection = new NpgsqlConnection(ConnectionString);
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

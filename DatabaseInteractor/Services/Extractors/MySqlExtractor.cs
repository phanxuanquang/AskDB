using DatabaseInteractor.Models.Enums;
using MySql.Data.MySqlClient;
using System.Data;

namespace DatabaseInteractor.Services.Extractors
{
    public class MySqlExtractor : ExtractorBase
    {
        public MySqlExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.MySQL;
        }

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(sqlQuery, connection);
            var dataTable = new DataTable();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(sqlQuery, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var permissions = new List<string>();
            var query = "SHOW GRANTS FOR CURRENT_USER()";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                using var command = new MySqlCommand(query, connection);
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
            using var connection = new MySqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
            }
            catch (MySqlException ex)
            {
                throw new InvalidOperationException(ex.Message, ex.InnerException);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}

using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
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

        private async Task<DataTable> ExecuteQueryAsync(NpgsqlCommand command)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            command.Connection = connection;

            await connection.OpenAsync();

            var dataTable = new DataTable();
            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            await using var command = new NpgsqlCommand(sqlQuery);
            return await ExecuteQueryAsync(command);
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = new NpgsqlCommand(sqlQuery, connection, transaction);
            try
            {
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var query = "SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = CURRENT_USER";
            var data = await ExecuteQueryAsync(query);
            return data.ToListString();
        }

        public override Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            throw new NotImplementedException();
        }

        public override async Task EnsureDatabaseConnectionAsync()
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
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
            var query = @"
                SELECT 
                    quote_ident(table_schema) || '.' || quote_ident(table_name) AS full_table_name
                FROM 
                    information_schema.tables
                WHERE 
                    table_type = 'BASE TABLE'
                    AND (table_schema NOT IN ('pg_catalog', 'information_schema'))
                    AND table_name ILIKE @keyword
                ORDER BY 
                    table_schema, table_name;";

            await using var command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }

        public override async Task<int> GetTableCountAsync()
        {
            var query = @"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_type = 'BASE TABLE' 
                AND table_schema NOT IN ('pg_catalog', 'information_schema');";
            await using var command = new NpgsqlCommand(query);
            await using var connection = new NpgsqlConnection(ConnectionString);
            command.Connection = connection;
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
    }
}

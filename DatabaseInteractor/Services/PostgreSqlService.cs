using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Npgsql;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class PostgreSqlService : DatabaseInteractionService
    {
        public PostgreSqlService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.PostgreSQL;
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

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            throw new NotImplementedException();
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var query = "SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = CURRENT_USER";
            var data = await ExecuteQueryAsync(query);
            return data.ToListString();
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
    }
}
using DatabaseInteractor.Models.Enums;
using System.Data;
using System.Data.SqlClient;

namespace DatabaseInteractor.Services.Extractors
{
    public class SqlServerExtractor : ExtractorBase
    {
        public SqlServerExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SqlServer;
        }
        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sqlQuery, connection);
            var dataTable = new DataTable();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sqlQuery, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var permissions = new List<string>();
            var query = "SELECT permission_name FROM fn_my_permissions(NULL, 'DATABASE')";

            using (var connection = new SqlConnection(ConnectionString))
            {
                using var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    permissions.Add(reader.GetString(0));
                }
            }
            return permissions;
        }

        public override async Task<List<string>> GetDatabaseSchemaNamesAsync(string? keyword)
        {
            var schemas = new List<string>();
            var query = "SELECT schema_name FROM information_schema.schemata";
            if (!string.IsNullOrEmpty(keyword))
            {
                query += " WHERE schema_name LIKE @keyword";
            }
            using (var connection = new SqlConnection(ConnectionString))
            {
                using var command = new SqlCommand(query, connection);
                if (!string.IsNullOrEmpty(keyword))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                }

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    schemas.Add(reader.GetString(0));
                }
            }
            return schemas;
        }

        public override async Task<DataTable> GetSchemaInfoAsync(string table, string? schema)
        {
            if (string.IsNullOrWhiteSpace(table)) throw new ArgumentException("Table name cannot be null or empty.", nameof(table));

            var dataTable = new DataTable();
            var query = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    kcu.CONSTRAINT_NAME,
                    tc.CONSTRAINT_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON c.TABLE_NAME = kcu.TABLE_NAME
                    AND c.COLUMN_NAME = kcu.COLUMN_NAME
                    AND c.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                    AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
                WHERE c.TABLE_NAME = @table
                    AND (@schema IS NULL OR c.TABLE_SCHEMA = @schema)
                ORDER BY c.ORDINAL_POSITION";

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@table", table);
            if (string.IsNullOrEmpty(schema)) command.Parameters.AddWithValue("@schema", DBNull.Value);
            else command.Parameters.AddWithValue("@schema", schema);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            return dataTable;
        }
    }
}

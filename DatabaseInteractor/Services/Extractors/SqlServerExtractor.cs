using AskDB.Commons.Enums;
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
                    permissions.Add(reader.GetString(0).ToLower());
                }
            }
            return permissions;
        }

        public override async Task<List<string>> SearchSchemasByNameAsync(string? keyword)
        {
            var schemas = new List<string>();
            var query = "SELECT schema_name FROM information_schema.schemata";
            if (!string.IsNullOrEmpty(keyword))
            {
                query += $" WHERE schema_name LIKE @keyword";
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

        public override async Task<DataTable> GetTableSchemaInfoAsync(string schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table)) throw new ArgumentException("Table name cannot be null or empty.", nameof(table));

            var dataTable = new DataTable();
            var query = @"
                SELECT
                    s.name AS SchemaName,
                    t.name AS TableName,
                    c.name AS ColumnName,
                    ty.name AS ColumnDataType,
                    c.is_nullable as IsColumnNullable,
                    c.max_length ColumnMaxLenght,
                    kc.type_desc AS ConstraintType,
                    ref_s.name AS ReferencedSchema,
                    ref_t.name AS ReferencedTable,
                    ref_c.name AS ReferencedColumn
                FROM sys.columns c
                JOIN sys.tables t ON c.object_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                LEFT JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.column_id = c.column_id
                LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = t.object_id AND ic.index_id = kc.unique_index_id
                LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = t.object_id AND fkc.parent_column_id = c.column_id
                LEFT JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
                LEFT JOIN sys.tables ref_t ON ref_t.object_id = fkc.referenced_object_id
                LEFT JOIN sys.schemas ref_s ON ref_s.schema_id = ref_t.schema_id
                LEFT JOIN sys.columns ref_c 
                    ON ref_c.object_id = fkc.referenced_object_id AND ref_c.column_id = fkc.referenced_column_id
                WHERE t.name = @table AND (@schema IS NULL OR s.name = @schema)
                ORDER BY c.column_id".Trim();

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

        public override async Task EnsureDatabaseConnectionAsync()
        {
            using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Message, ex.InnerException);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public override async Task<List<string>> SearchTablesByNameAsync(string schema, string? keyword)
        {
            var tables = new List<string>();
            var query = @"SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND (@schema IS NULL OR table_schema = @schema)";

            if (!string.IsNullOrEmpty(keyword))
            {
                query += " AND table_name LIKE @keyword";
            }
            using (var connection = new SqlConnection(ConnectionString))
            {
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@schema", string.IsNullOrEmpty(schema) ? DBNull.Value : schema);
                if (!string.IsNullOrEmpty(keyword))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                }
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            return tables;
        }

    }
}

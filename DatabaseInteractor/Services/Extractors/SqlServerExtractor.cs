using AskDB.Commons.Enums;
using System.Data;
using System.Data.SqlClient;
using System.Text;

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
            var query = "SELECT permission_name FROM fn_my_permissions(NULL, 'DATABASE') ORDER BY permission_name DESC";

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            var permissions = new List<string>();
            while (await reader.ReadAsync())
            {
                permissions.Add(reader.GetString(0).ToLower());
            }
            return permissions;
        }

        public override async Task<List<string>> SearchSchemasByNameAsync(string? keyword)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SELECT schema_name");
            sb.AppendLine("FROM information_schema.schemata");
            if (!string.IsNullOrEmpty(keyword))
            {
                sb.AppendLine("WHERE schema_name LIKE @keyword");
            }
            sb.AppendLine("ORDER BY schema_name ASC");

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sb.ToString(), connection);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            var schemas = new List<string>();
            while (await reader.ReadAsync())
            {
                schemas.Add(reader.GetString(0));
            }

            return schemas;
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table)) throw new ArgumentException("Table name cannot be null or empty.", nameof(table));

            var sb = new StringBuilder();
            sb.AppendLine("SELECT");
            sb.AppendLine("c.name AS ColumnName,");
            sb.AppendLine("ty.name AS ColumnDataType,");
            sb.AppendLine("c.is_nullable AS IsColumnNullable,");
            sb.AppendLine("c.max_length AS ColumnMaxLength,");
            sb.AppendLine("kc.type_desc AS ConstraintType,");
            sb.AppendLine("ref_s.name AS ReferencedTableSchema,");
            sb.AppendLine("ref_t.name AS ReferencedTable,");
            sb.AppendLine("ref_c.name AS ReferencedColumn");
            sb.AppendLine("FROM sys.columns c");
            sb.AppendLine("JOIN sys.tables t ON c.object_id = t.object_id");
            sb.AppendLine("JOIN sys.schemas s ON t.schema_id = s.schema_id");
            sb.AppendLine("JOIN sys.types ty ON c.user_type_id = ty.user_type_id");
            sb.AppendLine("LEFT JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.column_id = c.column_id");
            sb.AppendLine("LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = t.object_id AND ic.index_id = kc.unique_index_id");
            sb.AppendLine("LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = t.object_id AND fkc.parent_column_id = c.column_id");
            sb.AppendLine("LEFT JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id");
            sb.AppendLine("LEFT JOIN sys.tables ref_t ON ref_t.object_id = fkc.referenced_object_id");
            sb.AppendLine("LEFT JOIN sys.schemas ref_s ON ref_s.schema_id = ref_t.schema_id");
            sb.AppendLine("LEFT JOIN sys.columns ref_c ON ref_c.object_id = fkc.referenced_object_id AND ref_c.column_id = fkc.referenced_column_id");
            sb.AppendLine("WHERE t.name = @table AND s.name = @schema");
            sb.AppendLine("ORDER BY c.column_id");

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sb.ToString(), connection);

            command.Parameters.AddWithValue("@table", table);
            command.Parameters.AddWithValue("@schema", string.IsNullOrEmpty(schema) ? "dbo" : schema);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            var dataTable = new DataTable();
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
            var sb = new StringBuilder();
            sb.AppendLine("SELECT table_name");
            sb.AppendLine("FROM information_schema.tables");
            sb.AppendLine("WHERE table_type = 'BASE TABLE'");
            sb.AppendLine("AND table_schema = @schema");
            if (!string.IsNullOrEmpty(keyword))
            {
                sb.AppendLine("AND table_name LIKE @keyword");
            }

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sb.ToString(), connection);
            command.Parameters.AddWithValue("@schema", string.IsNullOrEmpty(schema) ? "dbo" : schema);
            if (!string.IsNullOrEmpty(keyword))
            {
                command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
            }
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

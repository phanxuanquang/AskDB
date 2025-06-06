using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class SqlServerService : DatabaseInteractionService
    {
        private readonly List<string> _systemSchemas =
        [
            "sys", "INFORMATION_SCHEMA", "db_owner", "db_accessadmin", "db_securityadmin",
            "db_backupoperator", "db_ddladmin", "db_datareader", "db_datawriter",
            "db_denydatareader", "db_denydatawriter"
        ];

        public SqlServerService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SqlServer;
        }

        public override async Task<int> GetTableCountAsync()
        {
            var query = @$"SELECT COUNT(*) FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND TABLE_SCHEMA NOT IN ({string.Join(',', $"'{_systemSchemas}'")})";

            await using var command = new SqlCommand(query);
            await using var connection = new SqlConnection(ConnectionString);
            command.Connection = connection;
            await connection.OpenAsync();

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrEmpty(table))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
            }

            var query = @" 
               SELECT
                   c.name AS ColumnName,   
                   ty.name AS ColumnDataType,   
                   c.is_nullable AS IsColumnNullable,   
                   c.max_length AS ColumnMaxLength,   
                   kc.type_desc AS ConstraintType,   
                   ref_s.name AS ReferencedTableSchema,   
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
                       LEFT JOIN sys.columns ref_c ON ref_c.object_id = fkc.referenced_object_id AND ref_c.column_id = fkc.referenced_column_id   
               WHERE t.name = @table AND s.name = @schema   
               ORDER BY c.column_id";

            await using var command = new SqlCommand(query);

            command.Parameters.AddWithValue("@table", table);
            command.Parameters.AddWithValue("@schema", string.IsNullOrEmpty(schema) ? "dbo" : schema);

            return await ExecuteQueryAsync(command);
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var query = "SELECT permission_name FROM fn_my_permissions(NULL, 'DATABASE')";
            var data = await ExecuteQueryAsync(query);
            return data.ToListString();
        }

        public override async Task<List<string>> SearchTablesByNameAsync(string? keyword, int maxResult = 20000)
        {
            var query = @$"
                SELECT TOP {maxResult} '[' + table_schema + '].[' + table_name + ']' as TableFullName
                FROM information_schema.tables 
                WHERE table_type = 'BASE TABLE' AND TABLE_SCHEMA NOT IN ({string.Join(',', $"'{_systemSchemas}'")}) AND table_name LIKE @keyword;";

            await using var command = new SqlCommand(query);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }
    }
}

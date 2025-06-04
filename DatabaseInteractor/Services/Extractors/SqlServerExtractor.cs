using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseInteractor.Services.Extractors
{
    public class SqlServerExtractor : ExtractorBase
    {
        private readonly List<string> _systemSchemas = new List<string>
        {
            "sys", "INFORMATION_SCHEMA", "db_owner", "db_accessadmin", "db_securityadmin",
            "db_backupoperator", "db_ddladmin", "db_datareader", "db_datawriter",
            "db_denydatareader", "db_denydatawriter"
        };

        public SqlServerExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SqlServer;
        }
        public async Task<DataTable> ExecuteQueryAsync(SqlCommand command)
        {
            await using var connection = new SqlConnection(ConnectionString);
            command.Connection = connection;

            await connection.OpenAsync();

            var dataTable = new DataTable();
            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await using var command = new SqlCommand(sqlQuery, connection);
            
            await connection.OpenAsync();
            var dataTable = new DataTable();
            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await using var command = new SqlCommand(sqlQuery, connection, (SqlTransaction)transaction);
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
            var query = "SELECT permission_name FROM fn_my_permissions(NULL, 'DATABASE')";
            var data = await ExecuteQueryAsync(query);
            return data.ToListString();
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

        public override async Task EnsureDatabaseConnectionAsync()
        {
            await using var connection = new SqlConnection(ConnectionString);

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

        public override async Task<List<string>> SearchTablesByNameAsync(string? keyword)
        {
            var query = @$"
                SELECT '[' + table_schema + '].[' + table_name + ']' as TableFullName
                FROM information_schema.tables 
                WHERE table_type = 'BASE TABLE' AND TABLE_SCHEMA NOT IN ({string.Join(',', $"'{_systemSchemas}'")}) AND table_name LIKE @keyword";

            await using var command = new SqlCommand(query);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
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
    }
}

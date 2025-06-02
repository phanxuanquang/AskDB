using AskDB.Commons.Enums;
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
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = new MySqlCommand(sqlQuery, connection, transaction);

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

        public override Task<List<string>> SearchSchemasByNameAsync(string? keyword)
        {
            throw new NotImplementedException();
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            var query = @"
                SELECT
                    cols.COLUMN_NAME AS ColumnName,
                    cols.DATA_TYPE AS ColumnDataType,
                    cols.IS_NULLABLE AS IsColumnNullable,
                    cols.CHARACTER_MAXIMUM_LENGTH AS ColumnMaxLength,
                    COALESCE(tc.CONSTRAINT_TYPE, '') AS ColumnConstraintType,
                    kcu.REFERENCED_TABLE_NAME AS ReferencedTable,
                    kcu.REFERENCED_COLUMN_NAME AS ReferencedColumn
                FROM
                    information_schema.COLUMNS cols
                LEFT JOIN information_schema.KEY_COLUMN_USAGE kcu
                    ON cols.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                    AND cols.TABLE_NAME = kcu.TABLE_NAME
                    AND cols.COLUMN_NAME = kcu.COLUMN_NAME
                LEFT JOIN information_schema.TABLE_CONSTRAINTS tc
                    ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
                    AND kcu.TABLE_NAME = tc.TABLE_NAME
                    AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                WHERE
                    cols.TABLE_SCHEMA = IFNULL(@schema, DATABASE())
                    AND cols.TABLE_NAME = @table
                ORDER BY
                    cols.ORDINAL_POSITION;";

            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@schema", (object?)schema ?? DBNull.Value);
            command.Parameters.AddWithValue("@table", table);

            var dataTable = new DataTable();
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            return dataTable;
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

        public override async Task<List<string>> SearchTablesByNameAsync(string? schema, string? keyword)
        {
            var query = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = IFNULL(@schema, DATABASE()) 
                    AND table_name COLLATE utf8mb4_general_ci LIKE CONCAT('%', @keyword, '%');";

            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@schema", (object?)schema ?? DBNull.Value);
            command.Parameters.AddWithValue("@table", keyword);

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            var table = new List<string>();
            while (await reader.ReadAsync())
            {
                table.Add(reader.GetString(0));
            }
            return table;
        }
    }
}

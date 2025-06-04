using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
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
            await using var connection = new MySqlConnection(ConnectionString);
            await using var command = new MySqlCommand(sqlQuery, connection);

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();
            var dataTable = new DataTable();
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
            var query = "SHOW GRANTS FOR CURRENT_USER()";
            var data = await ExecuteQueryAsync(query);
            return data.ToListString();
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

            await using var command = new MySqlCommand(query);
            command.Parameters.AddWithValue("@schema", (object?)schema ?? DBNull.Value);
            command.Parameters.AddWithValue("@table", table);

            return await ExecuteQueryAsync(command.CommandText);
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

        public override async Task<List<string>> SearchTablesByNameAsync(string? keyword)
        {
            var query = "SELECT table_name FROM information_schema.tables WHERE table_name COLLATE utf8mb4_general_ci LIKE CONCAT('%', @keyword, '%');";

            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@keyword", keyword);

            var data = await ExecuteQueryAsync(command.CommandText);
            return data.ToListString();
        }

        public override async Task<int> GetTableCountAsync()
        {
            var query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('mysql', 'information_schema', 'performance_schema', 'sys');";
            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(query, connection);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
    }
}

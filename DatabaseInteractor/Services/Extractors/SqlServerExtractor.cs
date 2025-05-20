using DatabaseInteractor.Models;
using DatabaseInteractor.Models.Enums;
using System.Collections.Concurrent;
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

        public override async Task<DataTable> GetSampleData(string tableName, string? schema, short maxRows = 10)
        {
            var query = $@"SELECT TOP {maxRows} * FROM [{schema ?? "dbo"}].[{tableName}] TABLESAMPLE ({maxRows * 3} ROWS);";
            return await ExecuteQueryAsync(query);
        }

        public override async Task<List<Table>> GetTablesAsync(string? tableNameFilter, string schema, int maxTables = 100)
        {
            using SqlConnection connection = new(ConnectionString);
            await connection.OpenAsync();

            var query = @$"
                DECLARE @SchemaName NVARCHAR(128) = '{schema ?? "dbo"}'; 
                DECLARE @TableNameFilter NVARCHAR(128) = '{tableNameFilter}'; 
                DECLARE @MaxRow INT = {maxTables}; 

                WITH TopTables AS (
                    SELECT TOP @MaxRow
                        t.object_id,
                        t.name AS TableName,
                        s.name AS SchemaName
                    FROM 
                        sys.tables t
                    JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.is_ms_shipped = 0
                        AND s.name = @SchemaName
                        AND t.name LIKE '%' + @TableNameFilter + '%'
                )

                SELECT 
                    tt.SchemaName,
                    tt.TableName,
                    c.name AS ColumnName,
                    ty.name AS DataType,
                    c.max_length AS MaxLength,
                    c.is_nullable AS IsNullable,
                    c.default_object_id,
                    pk.PrimaryKey,
                    fk.FK_Name,
                    fk.ParentColumn,
                    fk.ReferencedTable,
                    fk.ReferencedColumn
                FROM 
                    TopTables tt
                JOIN 
                    sys.columns c ON c.object_id = tt.object_id
                JOIN 
                    sys.types ty ON c.user_type_id = ty.user_type_id
                LEFT JOIN (
                    SELECT 
                        ic.object_id,
                        ic.column_id,
                        i.name AS PrimaryKey
                    FROM 
                        sys.indexes i
                    JOIN 
                        sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    WHERE 
                        i.is_primary_key = 1
                ) pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
                LEFT JOIN (
                    SELECT 
                        fk.name AS FK_Name,
                        fkc.parent_object_id,
                        fkc.parent_column_id,
                        fkc.referenced_object_id,
                        fkc.referenced_column_id,
                        cp.name AS ParentColumn,
                        OBJECT_NAME(fkc.referenced_object_id) AS ReferencedTable,
                        cr.name AS ReferencedColumn
                    FROM 
                        sys.foreign_keys fk
                    JOIN 
                        sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    JOIN 
                        sys.columns cp ON cp.object_id = fkc.parent_object_id AND cp.column_id = fkc.parent_column_id
                    JOIN 
                        sys.columns cr ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id
                ) fk ON fk.parent_object_id = c.object_id AND fk.parent_column_id = c.column_id";

            using SqlCommand command = new(query, connection);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            var tables = new ConcurrentDictionary<string, Table>();

            while (await reader.ReadAsync())
            {
                var schemaName = await reader.IsDBNullAsync(0) ? null : reader.GetString(0);
                var tableName = reader.GetString(1);
                var columnName = reader.GetString(2);
                var dataType = reader.GetString(3);
                var maxLength = await reader.IsDBNullAsync(4) ? (int?)null : reader.GetInt32(4);
                var isNullable = reader.GetString(5) == "YES";
                var defaultValue = await reader.IsDBNullAsync(6) ? null : reader.GetString(6);
                var primaryKey = await reader.IsDBNullAsync(7) ? null : reader.GetString(7);
                var fkName = await reader.IsDBNullAsync(8) ? null : reader.GetString(8);
                var parentColumn = await reader.IsDBNullAsync(9) ? null : reader.GetString(9);
                var referencedTable = await reader.IsDBNullAsync(10) ? null : reader.GetString(10);
                var referencedColumn = await reader.IsDBNullAsync(11) ? null : reader.GetString(11);

                var column = new Column
                {
                    Name = columnName,
                    DataType = dataType,
                    MaxLength = maxLength,
                    IsNullable = isNullable,
                    DefaultValue = defaultValue,
                    PrimaryKey = primaryKey,
                    ForeignKeyName = fkName,
                    ParentColumn = parentColumn,
                    ReferencedTable = referencedTable,
                    ReferencedColumn = referencedColumn
                };

                tables.AddOrUpdate(tableName,
                    new Table { Schema = schemaName, Name = tableName, Columns = [column] },
                    (_, table) =>
                    {
                        table.Columns.Add(column);
                        return table;
                    });
            }

            return [.. tables.Values];
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
    }
}

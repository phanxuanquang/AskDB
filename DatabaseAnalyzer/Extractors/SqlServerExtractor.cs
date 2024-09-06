using Dapper;
using DatabaseAnalyzer.Models;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;

namespace DatabaseAnalyzer.Extractors
{
    public class SqlServerExtractor : DatabaseExtractor
    {
        public SqlServerExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SqlServer;
            TableStructureQuery = @"
                SELECT t.TABLE_NAME, 
                       c.COLUMN_NAME, 
                       c.DATA_TYPE, 
                       c.CHARACTER_MAXIMUM_LENGTH, 
                       c.IS_NULLABLE, 
                       c.COLUMN_DEFAULT,
                       pk.COLUMN_NAME AS PRIMARY_KEY,
                       fk.FK_Name, 
                       fk.ParentColumn, 
                       OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable, 
                       fk.ReferencedColumn
                FROM INFORMATION_SCHEMA.TABLES t
                LEFT JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
                LEFT JOIN (
                    SELECT 
                        kcu.TABLE_NAME, 
                        kcu.COLUMN_NAME, 
                        kcu.CONSTRAINT_NAME AS PRIMARY_KEY
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    WHERE kcu.CONSTRAINT_NAME IN (
                        SELECT CONSTRAINT_NAME 
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                        WHERE CONSTRAINT_TYPE = 'PRIMARY KEY'
                    )
                ) pk ON t.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
                LEFT JOIN (
                    SELECT 
                        fk.name AS FK_Name,
                        tp.name AS ParentTable,
                        cp.name AS ParentColumn,
                        cr.name AS ReferencedColumn,
                        fk.referenced_object_id
                    FROM sys.foreign_keys AS fk
                    INNER JOIN sys.tables AS tp ON fk.parent_object_id = tp.object_id
                    INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
                    INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
                    INNER JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
                ) fk ON t.TABLE_NAME = fk.ParentTable AND c.COLUMN_NAME = fk.ParentColumn
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION";
        }

        public override async Task ExtractTables()
        {
            var tables = new ConcurrentDictionary<string, Table>();

            using (SqlConnection connection = new(ConnectionString))
            {
                await connection.OpenAsync();

                using SqlCommand command = new(TableStructureQuery, connection);
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    var columnName = reader.GetString(1);
                    var dataType = reader.GetString(2);
                    var maxLength = await reader.IsDBNullAsync(3) ? (int?)null : reader.GetInt32(3);
                    var isNullable = reader.GetString(4) == "YES";
                    var defaultValue = await reader.IsDBNullAsync(5) ? null : reader.GetString(5);
                    var primaryKey = await reader.IsDBNullAsync(6) ? null : reader.GetString(6);
                    var fkName = await reader.IsDBNullAsync(7) ? null : reader.GetString(7);
                    var parentColumn = await reader.IsDBNullAsync(8) ? null : reader.GetString(8);
                    var referencedTable = await reader.IsDBNullAsync(9) ? null : reader.GetString(9);
                    var referencedColumn = await reader.IsDBNullAsync(10) ? null : reader.GetString(10);

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
                        new Table { Name = tableName, Columns = [column] },
                        (_, table) =>
                        {
                            table.Columns.Add(column);
                            return table;
                        });
                }
            }

            Tables = [.. tables.Values];
        }

        public override async Task<DataTable> Execute(string sqlQuery)
        {
            using var connection = new SqlConnection(ConnectionString);
            var dataTable = new DataTable();
            await connection.OpenAsync();
            using (var reader = await connection.ExecuteReaderAsync(sqlQuery))
            {
                dataTable.Load(reader);
            }
            return dataTable;
        }
    }
}

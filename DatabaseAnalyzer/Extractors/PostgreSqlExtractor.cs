using Dapper;
using DatabaseAnalyzer.Models;
using Npgsql;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public class PostgreSqlExtractor : DatabaseExtractor
    {
        private static string ForeignKeyQuery => @"
        SELECT
            kcu.constraint_name AS ForeignKeyName,
            kcu.column_name AS ParentColumn,
            ccu.table_name AS ReferencedTable,
            ccu.column_name AS ReferencedColumn
        FROM
            information_schema.key_column_usage kcu
        JOIN
            information_schema.constraint_table_usage ctu
            ON kcu.constraint_name = ctu.constraint_name
        JOIN
            information_schema.constraint_column_usage ccu
            ON ctu.constraint_name = ccu.constraint_name
        WHERE
            kcu.table_schema = 'public'";

        public PostgreSqlExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.PostgreSQL;
            TableStructureQuery = @"
                SELECT 
                    table_schema || '.' || table_name AS table_name,
                    column_name, 
                    data_type, 
                    character_maximum_length, 
                    is_nullable, 
                    column_default
                FROM 
                    information_schema.columns
                WHERE 
                    table_schema = 'public'";
        }

        public override async Task ExtractTables()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var tables = new ConcurrentDictionary<string, Table>();

            var columns = await connection.QueryAsync<dynamic>(TableStructureQuery);

            var foreignKeys = await connection.QueryAsync<dynamic>(ForeignKeyQuery);

            foreach (var columnRow in columns)
            {
                var column = new Column
                {
                    Name = columnRow.column_name,
                    DataType = columnRow.data_type,
                    MaxLength = columnRow.character_maximum_length,
                    IsNullable = columnRow.is_nullable == "YES",
                    DefaultValue = columnRow.column_default
                };

                var fk = foreignKeys.FirstOrDefault(fkRow => fkRow.ParentColumn == columnRow.column_name && fkRow.ReferencedTable == (string)columnRow.table_name);
                if (fk != null)
                {
                    column.ForeignKeyName = fk.ForeignKeyName;
                    column.ParentColumn = fk.ParentColumn;
                    column.ReferencedTable = fk.ReferencedTable;
                    column.ReferencedColumn = fk.ReferencedColumn;
                }

                tables.AddOrUpdate((string)columnRow.table_name,
                    new Table
                    {
                        Name = (string)columnRow.table_name,
                        Columns = [column]
                    },
                    (_, table) =>
                    {
                        table.Columns.Add(column);
                        return table;
                    });
            }

            Tables = [.. tables.Values];
        }

        public override async Task<DataTable> Execute(string sqlQuery)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
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

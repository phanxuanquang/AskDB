using Dapper;
using DatabaseAnalyzer.Models;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public class MySqlExtractor : DatabaseExtractor
    {
        private const string ForeignKeyQuery = @"
            SELECT 
                kcu.constraint_name AS foreign_key_name,
                kcu.table_name AS parent_table,
                kcu.column_name AS parent_column,
                ccu.table_name AS referenced_table,
                ccu.column_name AS referenced_column
            FROM 
                information_schema.key_column_usage kcu
            JOIN 
                information_schema.constraint_column_usage ccu 
                ON kcu.constraint_name = ccu.constraint_name
            WHERE 
                kcu.table_schema = DATABASE()";

        public MySqlExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.MySQL;
            TableStructureQuery = @"
            SELECT 
                CONCAT(table_schema, '.', table_name) AS table_name,
                column_name, 
                data_type, 
                character_maximum_length, 
                is_nullable, 
                column_default
            FROM 
                information_schema.columns
            WHERE 
                table_schema = DATABASE()";
        }

        public override async Task ExtractTables()
        {
            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            var tables = new ConcurrentDictionary<string, Table>();
            var rows = await connection.QueryAsync<dynamic>(TableStructureQuery);

            foreach (var row in rows)
            {
                var column = new Column
                {
                    Name = (string)row.column_name,
                    DataType = (string)row.data_type,
                    MaxLength = (int?)row.character_maximum_length,
                    IsNullable = (string?)row.is_nullable == "YES",
                    DefaultValue = (string?)row.column_default,
                };

                var tableName = (string)row.table_name;
                tables.AddOrUpdate(tableName,
                    new Table { Name = tableName, Columns = [column] },
                    (_, table) =>
                    {
                        table.Columns.Add(column);
                        return table;
                    });
            }

            var foreignKeys = await connection.QueryAsync<dynamic>(ForeignKeyQuery);
            foreach (var fk in foreignKeys)
            {
                var parentColumnObj = tables[(string)fk.parent_table]?.Columns.FirstOrDefault(c => c.Name == fk.parent_column);
                if (parentColumnObj != null)
                {
                    parentColumnObj.ForeignKeyName = fk.foreign_key_name;
                    parentColumnObj.ParentColumn = fk.parent_column;
                    parentColumnObj.ReferencedTable = fk.referenced_table;
                    parentColumnObj.ReferencedColumn = fk.referenced_column;
                }
            }

            Tables = [.. tables.Values];
        }

        public override async Task<DataTable> Execute(string sqlQuery)
        {
            using var connection = new MySqlConnection(ConnectionString);
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

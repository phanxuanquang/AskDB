using Dapper;
using DatabaseInteractor.Models;
using DatabaseInteractor.Models.Enums;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseInteractor.Services.Extractors
{
    public class MySqlExtractor : ExtractorBase
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
        }

        public override async Task<DataTable> GetSampleData(string tableName, string? schema, short maxRows = 10)
        {
            var query = $@"SELECT * FROM `{schema ?? "dbo"}`.`{tableName}` LIMIT {maxRows}";
            return await ExecuteQueryAsync(query);
        }

        public override async Task<List<Table>> GetTablesAsync(string? tableNameFilter, string schema, int maxTables = 100)
        {
            using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @$"SET @schema_name = '{schema}';
                SET @keyword = '{tableNameFilter}';
                SET @limit_count = {maxTables};

                SELECT 
                    c.TABLE_SCHEMA as table_schema,
                    c.TABLE_NAME as table_name,
                    c.COLUMN_NAME as column_name,
                    c.DATA_TYPE as data_type,
                    c.CHARACTER_MAXIMUM_LENGTH as character_maximum_length,
                    c.IS_NULLABLE as is_nullable,
                    c.COLUMN_DEFAULT as column_default
                FROM 
                    INFORMATION_SCHEMA.COLUMNS c
                JOIN (
                    SELECT DISTINCT TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @schema_name
                      AND LOWER(TABLE_NAME) LIKE CONCAT('%', LOWER(@keyword), '%')
                    LIMIT @limit_count
                ) t ON c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = @schema_name
                WHERE 
                    c.TABLE_SCHEMA = @schema_name";
            var rows = await connection.QueryAsync<dynamic>(query);

            var tables = new ConcurrentDictionary<string, Table>();

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
                    new Table
                    {
                        Schema = (string?)row.table_schema,
                        Name = tableName,
                        Columns = [column]
                    },
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

            return [.. tables.Values];
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
    }
}

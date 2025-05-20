using Dapper;
using DatabaseInteractor.Models;
using DatabaseInteractor.Models.Enums;
using Npgsql;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseInteractor.Services.Extractors
{
    public class PostgreSqlExtractor : ExtractorBase
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
        }

        public override async Task<List<Table>> GetTablesAsync(string? tableNameFilter, string schema, int maxTables = 100)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var tables = new ConcurrentDictionary<string, Table>();

            var query = @$"WITH selected_tables AS (
                SELECT 
                    table_schema,
                    table_name
                FROM 
                    information_schema.tables
                WHERE 
                    table_type = 'BASE TABLE'
                    AND table_schema = '{schema}'
                    AND table_name LIKE '%{tableNameFilter}%'
                LIMIT {maxTables}
            )
            SELECT 
                c.table_schema,
                c.table_name,
                c.column_name,
                c.data_type,
                c.character_maximum_length,
                c.is_nullable,
                c.column_default
            FROM 
                information_schema.columns c
            JOIN 
                selected_tables st
                ON c.table_schema = st.table_schema AND c.table_name = st.table_name";

            var columns = await connection.QueryAsync<dynamic>(query);

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
                        Schema = (string?)columnRow.table_schema,
                        Name = (string)columnRow.table_name,
                        Columns = [column]
                    },
                    (_, table) =>
                    {
                        table.Columns.Add(column);
                        return table;
                    });
            }

            return [.. tables.Values];
        }

        public override async Task<DataTable> GetSampleData(string tableName, string? schema, short maxRows = 10)
        {
            var query = @$"SELECT *
            FROM (
              SELECT *
              FROM ""{schema ?? "public"}"".""{tableName}""
              TABLESAMPLE SYSTEM (1)
            ) AS t LIMIT {maxRows}";

            return await ExecuteQueryAsync(query);
        }

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            using var command = new NpgsqlCommand(sqlQuery, connection);
            var dataTable = new DataTable();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }
    }
}

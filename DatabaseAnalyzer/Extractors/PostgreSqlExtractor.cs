using Dapper;
using DatabaseAnalyzer.Models;
using Npgsql;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public class PostgreSqlExtractor : DatabaseExtractor
    {
        public PostgreSqlExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.PostgreSQL;
            TableStructureQuery = @"
                 SELECT table_name, column_name, data_type, character_maximum_length, is_nullable, column_default 
                 FROM information_schema.columns 
                 WHERE table_schema = 'public' 
                 ORDER BY table_name, ordinal_position";
        }

        public override async Task ExtractTables()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var tables = new ConcurrentDictionary<string, Table>();
            var rows = await connection.QueryAsync<dynamic>(TableStructureQuery);

            foreach (var row in rows)
            {
                var column = new Column
                {
                    Name = row.column_name,
                    DataType = row.data_type,
                    MaxLength = row.character_maximum_length,
                    IsNullable = row.is_nullable == "YES",
                    DefaultValue = row.column_default
                };

                tables.AddOrUpdate((string)row.table_name,
                    new Table
                    {
                        Name = (string)row.table_name,
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

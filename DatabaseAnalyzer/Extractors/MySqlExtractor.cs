using Dapper;
using DatabaseAnalyzer.Models;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public class MySqlExtractor : DatabaseExtractor
    {
        public MySqlExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.MySQL;
            TableStructureQuery = @"
                    SELECT table_name, column_name, data_type, character_maximum_length, is_nullable, column_default 
                    FROM information_schema.columns 
                    WHERE table_schema = DATABASE() 
                    ORDER BY table_name, ordinal_position";
        }

        public override async Task ExtractTables()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                var tables = new ConcurrentDictionary<string, Table>();

                var rows = await connection.QueryAsync<dynamic>(TableStructureQuery);
                foreach (var row in rows)
                {
                    string tableName = row.table_name;
                    string columnName = row.column_name;
                    string dataType = row.data_type;
                    int? maxLength = row.character_maximum_length;
                    bool isNullable = row.is_nullable == "YES";
                    string defaultValue = row.column_default;

                    var column = new Column
                    {
                        Name = columnName,
                        DataType = dataType,
                        MaxLength = maxLength,
                        IsNullable = isNullable,
                        DefaultValue = defaultValue
                    };

                    tables.AddOrUpdate(tableName,
                        new Table { Name = tableName, Columns = new List<Column> { column } },
                        (_, table) =>
                        {
                            table.Columns.Add(column);
                            return table;
                        });
                }

                Tables = tables.Values.ToList();
            }
        }

        public override async Task<DataTable> Execute(string sqlQuery)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
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

}

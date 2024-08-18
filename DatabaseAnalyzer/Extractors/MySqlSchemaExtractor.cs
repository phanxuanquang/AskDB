using Dapper;
using DatabaseAnalyzer.Models;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;

namespace DatabaseAnalyzer.Extractors
{
    public class MySqlSchemaExtractor : IDatabaseSchemaExtractor
    {
        public async Task<List<Table>> GetDatabaseStructureAsync(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string fullSchemaQuery = @"
                SELECT table_name, column_name, data_type, 
                       character_maximum_length, is_nullable, column_default 
                FROM information_schema.columns 
                WHERE table_schema = DATABASE() 
                ORDER BY table_name, ordinal_position";

                var tables = new ConcurrentDictionary<string, Table>();

                var rows = await connection.QueryAsync<dynamic>(fullSchemaQuery);
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
                        _ => new Table { Name = tableName, Columns = new List<Column> { column } },
                        (_, table) =>
                        {
                            table.Columns.Add(column);
                            return table;
                        });
                }

                return tables.Values.ToList();
            }
        }
    }

}

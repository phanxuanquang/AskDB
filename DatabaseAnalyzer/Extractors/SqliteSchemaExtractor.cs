using Dapper;
using DatabaseAnalyzer.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

namespace DatabaseAnalyzer.Extractors
{
    public class SqliteSchemaExtractor : IDatabaseSchemaExtractor
    {
        public async Task<List<Table>> GetDatabaseStructureAsync(string connectionString)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                string fullSchemaQuery = @"
                SELECT name FROM sqlite_master 
                WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";

                var tables = new ConcurrentDictionary<string, Table>();
                var tableNames = await connection.QueryAsync<string>(fullSchemaQuery);

                foreach (var tableName in tableNames)
                {
                    var columns = await connection.QueryAsync<dynamic>($"PRAGMA table_info({tableName})");
                    var table = new Table { Name = tableName };

                    foreach (var column in columns)
                    {
                        table.Columns.Add(new Column
                        {
                            Name = column.name,
                            DataType = column.type,
                            IsNullable = !column.notnull,
                            DefaultValue = column.dflt_value,
                            PrimaryKey = column.pk == 1 ? column.name : null
                        });
                    }

                    tables.TryAdd(tableName, table);
                }

                return tables.Values.ToList();
            }
        }
    }

}

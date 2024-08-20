using Dapper;
using DatabaseAnalyzer.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public class SqliteExtractor : IDatabaseExtractor
    {
        public async Task<List<Table>> GetTables(string connectionString)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";

                var tables = new ConcurrentDictionary<string, Table>();
                var tableNames = await connection.QueryAsync<string>(query);

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

        public async Task<DataTable> GetData(string connectionString, string sqlQuery)
        {
            using (var connection = new SqliteConnection(connectionString))
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

using Dapper;
using DatabaseAnalyzer.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public class SqliteExtractor : IDatabaseExtractor
    {
        private sealed class ColumnInfor
        {
            public int Cid { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public bool NotNull { get; set; }
            public string? DefaultValue { get; set; }
            public int? Pk { get; set; }
        }

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
                    var columns = await connection.QueryAsync<ColumnInfor>($"PRAGMA table_info({tableName})");

                    var table = new Table
                    {
                        Name = tableName,
                        Columns = columns.Select(column => new Column
                        {
                            Name = column.Name,
                            DataType = column.Type,
                            IsNullable = !column.NotNull,
                            DefaultValue = column.DefaultValue,
                            PrimaryKey = column.Pk == 1 ? column.Name : null
                        }).ToList()
                    };

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

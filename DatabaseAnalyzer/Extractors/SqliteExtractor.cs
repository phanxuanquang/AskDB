using Dapper;
using DatabaseAnalyzer.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public class SqliteExtractor : DatabaseExtractor
    {
        internal class ColumnInfor
        {
            internal int Cid { get; set; }
            internal required string Name { get; set; }
            internal required string Type { get; set; }
            internal bool NotNull { get; set; }
            internal string? DefaultValue { get; set; }
            internal int? Pk { get; set; }
        }

        internal class ForeignKeyInfo
        {
            internal int Id { get; set; }
            internal string Table { get; set; }
            internal string From { get; set; }
            internal string To { get; set; }
        }

        public SqliteExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SQLite;
            TableStructureQuery = @"SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
        }

        public override async Task ExtractTables()
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var tables = new ConcurrentDictionary<string, Table>();
            var tableNames = await connection.QueryAsync<string>(TableStructureQuery);

            foreach (var tableName in tableNames)
            {
                var columnInfors = await connection.QueryAsync<ColumnInfor>($"PRAGMA table_info({tableName})");

                var foreignKeyInfors = await connection.QueryAsync<ForeignKeyInfo>($"PRAGMA foreign_key_list({tableName})");

                var table = new Table
                {
                    Name = tableName,
                    Columns = columnInfors
                        .Select(column =>
                        {
                            var foreignKeyInfor = foreignKeyInfors.FirstOrDefault(fk => fk.From.Equals(column.Name));

                            return new Column
                            {
                                Name = column.Name,
                                DataType = column.Type,
                                IsNullable = !column.NotNull,
                                DefaultValue = column.DefaultValue,
                                PrimaryKey = column.Pk == 1 ? column.Name : null,
                                ForeignKeyName = foreignKeyInfor?.Table, 
                                ParentColumn = foreignKeyInfor?.From,    
                                ReferencedTable = foreignKeyInfor?.Table, 
                                ReferencedColumn = foreignKeyInfor?.To  
                            };
                        })
                        .ToList()
                };

                tables.TryAdd(tableName, table);
            }

            Tables = [.. tables.Values];
        }

        public override async Task<DataTable> Execute(string sqlQuery)
        {
            using var connection = new SqliteConnection(ConnectionString);
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

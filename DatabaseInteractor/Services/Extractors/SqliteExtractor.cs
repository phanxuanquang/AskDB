using Dapper;
using DatabaseInteractor.Models;
using DatabaseInteractor.Models.Enums;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;

namespace DatabaseInteractor.Services.Extractors
{
    public class SqliteExtractor : ExtractorBase
    {
        #region Internal Classes
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
        #endregion

        public SqliteExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SQLite;
        }

        public override async Task<List<Table>> GetTablesAsync(string? tableNameFilter, string schema, int maxTables = 100)
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            var tables = new ConcurrentDictionary<string, Table>();
            var tableNameList = await connection.QueryAsync<string>(@"SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'");

            foreach (var tableName in tableNameList)
            {
                var columnInfors = await connection.QueryAsync<ColumnInfor>($"PRAGMA table_info({tableName})");

                var foreignKeyInfors = await connection.QueryAsync<ForeignKeyInfo>($"PRAGMA foreign_key_list({tableName})");

                var table = new Table
                {
                    Schema = null,
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

            return [.. tables.Values];
        }

        public override async Task<DataTable> GetSampleData(string tableName, string? schema, short maxRows = 10)
        {
            var query = $@"SELECT * FROM {tableName} ORDER BY RANDOM() LIMIT {maxRows}";
            return await ExecuteQueryAsync(query);
        }

        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            using var connection = new SqliteConnection(ConnectionString);
            using var command = new SqliteCommand(sqlQuery, connection);
            var dataTable = new DataTable();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }
    }
}

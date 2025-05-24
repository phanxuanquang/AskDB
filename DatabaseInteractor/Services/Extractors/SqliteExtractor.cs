using DatabaseInteractor.Models.Enums;
using Microsoft.Data.Sqlite;
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

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            using var connection = new SqliteConnection(ConnectionString);
            using var command = new SqliteCommand(sqlQuery, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public override Task<List<string>> GetUserPermissionsAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<List<string>> GetDatabaseSchemaNamesAsync(string? keyword)
        {
            throw new NotImplementedException();
        }

        public override Task<DataTable> GetSchemaInfoAsync(string schema, string table)
        {
            throw new NotImplementedException();
        }
    }
}

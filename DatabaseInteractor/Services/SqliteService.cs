using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Microsoft.Data.Sqlite;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class SqliteService : DatabaseInteractionService
    {
        public SqliteService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SQLite;
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrEmpty(table))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
            }
            var query = GetTableStructureDetailQueryTemplate.Replace("{TableName}", table);
            return await ExecuteQueryAsync(query);
        }

        public override Task<List<string>> GetUserPermissionsAsync()
        {
            return
            Task.FromResult<List<string>>([
                "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER", "INDEX", "TRIGGER"
            ]);
        }

        public override async Task<List<string>> SearchTablesByNameAsync(string? keyword, int? maxResult = 20000)
        {
            if (CachedAllTableNames.Count != 0)
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    return [.. CachedAllTableNames];
                }
                else
                {
                    return SearchTablesFromCachedTableNames(keyword);
                }
            }

            var query = SearchTablesByNameQueryTemplate.Replace("{MaxResultParam}", maxResult.HasValue ? $"LIMIT {maxResult.Value}" : string.Empty);

            await using var command = new SqliteCommand(query);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }
    }
}

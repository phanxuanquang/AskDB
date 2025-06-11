using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using MySqlConnector;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class MySqlService : DatabaseInteractionService
    {
        public MySqlService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.MySQL;
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrEmpty(table))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
            }

            await using var command = new MySqlCommand(GetTableStructureDetailQueryTemplate);
            command.Parameters.AddWithValue("@table", table);

            return await ExecuteQueryAsync(command);
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var query = "SHOW GRANTS FOR CURRENT_USER()";
            var data = await ExecuteQueryAsync(query);
            return data.ToListString();
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

            using var command = new MySqlCommand(query);
            command.Parameters.AddWithValue("@keyword", keyword);

            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }
    }
}

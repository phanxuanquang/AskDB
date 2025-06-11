using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Npgsql;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class PostgreSqlService : DatabaseInteractionService
    {
        public PostgreSqlService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.PostgreSQL;
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrEmpty(table))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
            }

            await using var command = new NpgsqlCommand(GetTableStructureDetailQueryTemplate);
            command.Parameters.AddWithValue("@table", table);
            command.Parameters.AddWithValue("@schema", string.IsNullOrEmpty(schema) ? "public" : schema);
            return await ExecuteQueryAsync(command);
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var query = "SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = CURRENT_USER";
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

            await using var command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }
    }
}
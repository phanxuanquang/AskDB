using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class SqlServerService : DatabaseInteractionService
    {
        public SqlServerService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SqlServer;
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrEmpty(table))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
            }

            await using var command = new SqlCommand(GetTableStructureDetailQueryTemplate);

            command.Parameters.AddWithValue("@table", table);
            command.Parameters.AddWithValue("@schema", string.IsNullOrEmpty(schema) ? "dbo" : schema);

            return await ExecuteQueryAsync(command);
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var query = "SELECT permission_name FROM fn_my_permissions(NULL, 'DATABASE')";
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

            var query = SearchTablesByNameQueryTemplate.Replace("{MaxResultParam}", maxResult.HasValue ? $"TOP {maxResult.Value}" : string.Empty);

            await using var command = new SqlCommand(query);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }
    }
}

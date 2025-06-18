using AskDB.Commons.Enums;
using DatabaseInteractor.Factories;
using DatabaseInteractor.Helpers;
using System.Data;
using System.Data.Common;

namespace DatabaseInteractor.Services
{
    public abstract class DatabaseInteractionService(string connectionString)
    {
        public HashSet<string> CachedAllTableNames { get; set; } = [];
        protected string SearchTablesByNameQueryTemplate { get; private set; }
        public string GetTableStructureDetailQueryTemplate { get; private set; }

        public DatabaseType DatabaseType { get; protected set; }
        public string ConnectionString { get; } = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        protected DbConnection GetConnection() => DatabaseType.CreateDbConnection(ConnectionString);
        public async Task EnsureDatabaseConnectionAsync()
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();
        }

        protected async Task<DataTable> ExecuteQueryAsync(DbCommand command)
        {
            await using var connection = GetConnection();
            command.Connection ??= connection;

            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync();

            var dataTable = new DataTable();
            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            return dataTable;
        }

        public async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            await using var connection = GetConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.Connection = connection;

            return await ExecuteQueryAsync(command);
        }

        public async Task ExecuteNonQueryAsync(string sqlCommand)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sqlCommand;
                command.Transaction = transaction;

                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public abstract Task<List<string>> GetUserPermissionsAsync();

        public abstract Task<List<string>> SearchTablesByNameAsync(string? keyword, int? maxResult = 20000);

        public abstract Task<DataTable> GetTableStructureDetailAsync(string? schema, string table);

        protected List<string> SearchTablesFromCachedTableNames(string keyword)
        {
            var searcher = new TableNameSearcher(CachedAllTableNames.ToList());

            return searcher.SearchByThreshold(keyword, 65);
        }

        public async Task InitQueryTemplatesAsync()
        {
            var searchTablesTask = OnlineContentHelper.GeSqlContentAsync(DatabaseType, nameof(SearchTablesByNameAsync));
            var getTableStructureTask = OnlineContentHelper.GeSqlContentAsync(DatabaseType, nameof(GetTableStructureDetailAsync));

            await Task.WhenAll(searchTablesTask, getTableStructureTask);

            SearchTablesByNameQueryTemplate = searchTablesTask.Result;
            GetTableStructureDetailQueryTemplate = getTableStructureTask.Result;
        }
    }
}

using AskDB.Commons.Enums;
using System.Data;

namespace DatabaseInteractor.Services
{
    public abstract class ExtractorBase(string connectionString)
    {
        protected string ConnectionString = connectionString;
        public DatabaseType DatabaseType { get; protected set; }

        public abstract Task<DataTable> ExecuteQueryAsync(string sqlQuery);
        public abstract Task ExecuteNonQueryAsync(string sqlQuery);
        public abstract Task<List<string>> GetUserPermissionsAsync();
        public abstract Task<List<string>> GetDatabaseSchemaNamesAsync(string? keyword);
        public abstract Task<DataTable> GetSchemaInfoAsync(string table, string? schema);
        public abstract Task EnsureDatabaseConnectionAsync();
    }
}

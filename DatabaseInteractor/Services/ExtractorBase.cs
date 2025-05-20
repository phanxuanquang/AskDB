using DatabaseInteractor.Models;
using DatabaseInteractor.Models.Enums;
using System.Data;

namespace DatabaseInteractor.Services
{
    public abstract class ExtractorBase(string connectionString)
    {
        protected string ConnectionString = connectionString;
        protected DatabaseType DatabaseType;

        public abstract Task<List<Table>> GetTablesAsync(string? tableNameFilter, string schema, int maxTables = 100);

        public abstract Task<DataTable> ExecuteQueryAsync(string sqlQuery);

        public abstract Task<DataTable> GetSampleData(string tableName, string? schema, short maxRows = 10);
    }
}

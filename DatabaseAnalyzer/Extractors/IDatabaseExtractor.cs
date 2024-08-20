using DatabaseAnalyzer.Models;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public interface IDatabaseExtractor
    {
        Task<List<Table>> GetTables(string connectionString);
        Task<DataTable> GetData(string connectionString, string sqlQuery);
    }
}

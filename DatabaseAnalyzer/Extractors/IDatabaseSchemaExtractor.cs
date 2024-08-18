using DatabaseAnalyzer.Models;

namespace DatabaseAnalyzer.Extractors
{
    public interface IDatabaseSchemaExtractor
    {
        Task<List<Table>> GetDatabaseStructureAsync(string connectionString);
    }

}

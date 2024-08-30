using DatabaseAnalyzer.Models;
using System.Data;

namespace DatabaseAnalyzer.Extractors
{
    public abstract class DatabaseExtractor(string connectionString)
    {
        public string TableStructureQuery { get; set; }
        public string ConnectionString { get; set; } = connectionString;
        public DatabaseType DatabaseType { get; set; }
        public List<Table> Tables { get; set; } = new List<Table>();

        public abstract Task ExtractTables();
        public abstract Task<DataTable> GetData(string sqlQuery);
    }
}

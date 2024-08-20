using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;

namespace DatabaseAnalyzer
{
    public static class Analyzer
    {
        public const byte MaxTotalTables = 100;
        public const short MaxTotalColumns = 500;

        public static async Task<List<Table>> GetTables(DatabaseType databaseType, string connectionString)
        {
            IDatabaseExtractor schemaExtractor;
            List<Table> tables;

            switch (databaseType)
            {
                case DatabaseType.MSSQL:
                    schemaExtractor = new SqlServerExtractor();
                    break;
                case DatabaseType.PostgreSQL:
                    schemaExtractor = new PostgreSqlExtractor();
                    break;
                case DatabaseType.SQLite:
                    schemaExtractor = new SqliteExtractor();
                    break;
                case DatabaseType.MariaDB:
                case DatabaseType.MySQL:
                    schemaExtractor = new MySqlExtractor();
                    break;
                default:
                    throw new NotSupportedException("Not Supported");
            }

            tables = await schemaExtractor.GetTables(connectionString);

            return tables;
        }

        public static string TablesAsString(List<Table> tables)
        {
            var schemas = tables.Select(d => d.ToString()).ToList();
            return string.Join(string.Empty, schemas).Trim();
        }
    }
}

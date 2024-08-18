using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;

namespace DatabaseAnalyzer
{
    public static class Analyzer
    {
        public const byte MaxTotalTables = 100;
        public const short MaxTotalColumns = 500;
        public static async Task<List<Table>> GetDatabaseSchemas(DatabaseType databaseType, string connectionString)
        {
            IDatabaseSchemaExtractor schemaExtractor;
            List<Table> tables;

            switch (databaseType)
            {
                case DatabaseType.MSSQL:
                    schemaExtractor = new SqlServerSchemaExtractor();
                    break;
                case DatabaseType.PostgreSQL:
                    schemaExtractor = new PostgreSqlSchemaExtractor();
                    break;
                case DatabaseType.SQLite:
                    schemaExtractor = new SqliteSchemaExtractor();
                    break;
                case DatabaseType.MariaDB:
                case DatabaseType.MySQL:
                    schemaExtractor = new MySqlSchemaExtractor();
                    break;
                default:
                    throw new NotSupportedException("Not Supported");
            }

            tables = await schemaExtractor.GetDatabaseStructureAsync(connectionString);

            return tables;
        }
    }
}

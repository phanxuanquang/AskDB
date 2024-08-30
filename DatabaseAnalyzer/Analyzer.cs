using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using GenAI;
using Helper;
using Newtonsoft.Json;
using System.Text;

namespace DatabaseAnalyzer
{
    public static class Analyzer
    {
        public const byte MaxTotalTables = 100;
        public const short MaxTotalColumns = 500;
        public static string ApiKey = string.Empty;
        public static string ConnectionString = string.Empty;
        public static DatabaseType DatabaseType = DatabaseType.MSSQL;
        public static List<Table> Tables = new List<Table>();
        public static bool IsActivated = false;


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
                case DatabaseType.MySQL:
                    schemaExtractor = new MySqlExtractor();
                    break;
                default:
                    throw new NotSupportedException("Not Supported");
            }

            tables = await schemaExtractor.GetTables(connectionString);

            return tables.OrderBy(t => t.Name).ToList();
        }

        public static string TablesAsString(List<Table> tables)
        {
            var schemas = tables.Select(d => d.ToString()).ToList();
            return string.Join(string.Empty, schemas).Trim();
        }

        public static async Task<SqlCommander> GetSql(string apiKey, List<Table> tables, string question, DatabaseType type)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = Extractor.GetEnumDescription(type);

            promptBuilder.AppendLine($"You are a Database Administrator with over 20 years of experience working with {databaseType} databases on large scale projects.");
            promptBuilder.AppendLine("I am someone who knows nothing about SQL.");
            promptBuilder.AppendLine($"I will provide you with the structure of my database and my query in natural language. Please help me convert it into a corresponding {databaseType} query.");
            promptBuilder.AppendLine("Your response must include two parts as follows:");
            promptBuilder.AppendLine($"- Output: This is your response to my input. If my input cannot be converted into a {databaseType} query or you find it is not relevant to the table structure in the database I provided or not a query or you find it is an INSERT/UPDATE/DELETE query, please respond that my request is invalid. Otherwise, please return the corresponding {databaseType} query. In addition, the SQL command must be well-formated for me to read and understand.");
            promptBuilder.AppendLine("- IsSql: If the Output is an SQL query, this should be TRUE; otherwise, it should be FALSE.");
            promptBuilder.AppendLine("Your response should be a JSON that corresponds to the following C# class:");
            promptBuilder.AppendLine("class SqlCommander");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("    string Output;");
            promptBuilder.AppendLine("    bool IsSql;");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("To help you understand my command and do the task more effectively, here is an example:");
            promptBuilder.AppendLine("My input: give me all available products");
            promptBuilder.AppendLine("Your response:");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("    \"Output\" : \"SELECT * AS AvailableProducts FROM Products WHERE IsAvailable = 1\",");
            promptBuilder.AppendLine("    \"IsSql\" : true");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("Now, let's get started.");
            promptBuilder.AppendLine("This is the table schemas of my database:");
            promptBuilder.AppendLine(TablesAsString(tables).Trim());
            promptBuilder.AppendLine($"My input: {question}");
            promptBuilder.AppendLine("Your response:");

            var response = await Generator.GenerateContent(apiKey, promptBuilder.ToString(), true, CreativityLevel.Medium, GenerativeModel.Gemini_15_Flash);
            return JsonConvert.DeserializeObject<SqlCommander>(response);
        }
    }
}

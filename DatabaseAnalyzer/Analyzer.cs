using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using GenAI;
using Helper;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace DatabaseAnalyzer
{
    public static class Analyzer
    {
        public const byte MaxTotalTables = 100;
        public const short MaxTotalColumns = 500;
        public const byte MaxTotalQueries = 50;
        public static List<Table> SelectedTables = new List<Table>();
        public static DatabaseExtractor DatabaseExtractor;

        public static string TablesAsString(List<Table> tables)
        {
            var schemas = tables.Select(d => d.ToString()).ToList();
            return string.Join(string.Empty, schemas).Trim();
        }

        public static async Task<SqlCommander> GetSql(string apiKey, string question, DatabaseType type)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = Extractor.GetEnumDescription(type);

            promptBuilder.AppendLine($"You are a Database Administrator with over 20 years of experience working with {databaseType} databases on large scale projects.");
            promptBuilder.AppendLine("I am someone who knows nothing about SQL.");
            promptBuilder.AppendLine($"I will provide you with the structure of my database and my query in natural language. Please help me convert it into a corresponding {databaseType} query.");
            promptBuilder.AppendLine("Your response must include two parts as follows:");
            promptBuilder.AppendLine($"- Output: This is your response to my input. If my input cannot be converted into a {databaseType} query or you find it is not relevant to the table structure in the database I provided, please respond that my request is invalid and why. Otherwise, please return the corresponding {databaseType} query.");
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
            promptBuilder.AppendLine(TablesAsString(SelectedTables).Trim());
            promptBuilder.AppendLine($"My input: {question}");
            promptBuilder.AppendLine("Your response:");

            var response = await Generator.GenerateContent(apiKey, promptBuilder.ToString(), true, CreativityLevel.Medium, GenerativeModel.Gemini_15_Flash);
            return JsonConvert.DeserializeObject<SqlCommander>(response);
        }

        public static async Task<List<string>> GetSuggestedQueries(string apiKey, DatabaseType type, bool useSql)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = Extractor.GetEnumDescription(type);
            var englishQuery = !useSql ? $"human language ({CultureInfo.CurrentCulture.EnglishName.Split(' ')[0]})" : databaseType;

            promptBuilder.AppendLine($"You are a Database Administrator with over 20 years of experience working with {databaseType} databases on large scale projects.");
            promptBuilder.AppendLine("I am someone who knows nothing about SQL.");
            promptBuilder.AppendLine($"I will provide you with the table structure of my database. You have to suggest at least {MaxTotalQueries} common {englishQuery} queries related to my database structure.");
            promptBuilder.AppendLine("Your response must be a List<string> in C# programming language.");
            promptBuilder.AppendLine("To help you understand my command and do the task more effectively, here is an example:");
            promptBuilder.AppendLine("Your response:");
            promptBuilder.AppendLine("[");
            if (useSql)
            {
                promptBuilder.AppendLine($"    SELECT * FROM Table1 WHERE Condition,");
                promptBuilder.AppendLine($"    SELECT COUNT(*) FROM Table2 WHERE Condition,");
                promptBuilder.AppendLine($"    SELECT DISTINCT(*) FROM Table3 WHERE Condition OrderBy Id DESC");
            }
            else
            {
                promptBuilder.AppendLine($"    Give me all items of the ExampleTableName table,");
                promptBuilder.AppendLine($"    How many ExampleTableName items that <some_conditions>?,");
                promptBuilder.AppendLine($"    I want to know 10 latest item of the ExampleTableName that <some_conditions>,");
                promptBuilder.AppendLine($"    Tell me the items of the ExampleTableName that <some_conditions> after <some_date>");
            }
            promptBuilder.AppendLine("]");
            promptBuilder.AppendLine("Now, let's get started. This is the table schemas of my database:");
            promptBuilder.AppendLine(TablesAsString(SelectedTables).Trim());
            promptBuilder.AppendLine("Your response:");

            var response = await Generator.GenerateContent(apiKey, promptBuilder.ToString(), true, CreativityLevel.Medium, GenerativeModel.Gemini_15_Flash);
            return JsonConvert.DeserializeObject<List<string>>(response);
        }

        public static bool IsSqlSafe(string sqlCommand)
        {
            var unsafeKeywords = new string[]
            {
                "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE",
                "CREATE", "EXEC", "EXECUTE", "SP_EXECUTESQL", "XPCMDSHELL",
                "SYSCOLUMNS", "SYSOBJECTS", "SYSUSERS", "GRANT", "REVOKE",
                "DENY", "ADD", "SET", "INTO", "OPENROWSET", "OPENQUERY",
                "OPENDATASOURCE", "BACKUP", "RESTORE", "RECONFIGURE",
                "SHUTDOWN", "KILL", "DBCC", "BULK INSERT", "UPDATETEXT",
                "WRITETEXT", "LOCK", "CHECKPOINT", "PARTITION", "REINDEX",
                "REVERT", "ROLLBACK", "SAVE", "SECURITYAUDIT", "TRIGGER",
                "FUNC", "PROCEDURE", "VIEW", "INDEX", "CONSTRAINT", "SCHEMA",
                "DATABASE", "TABLE", "TRANSACTION", "USE", "OPEN", "FETCH",
                "DEALLOCATE", "DECLARE", "RAISERROR", "DISABLE", "ENABLE",
                "ASMRESTORE", "ASSEMBLYPROPERTY", "ADDMEMBER", "DROPFACET",
                "ADDROLE", "ADDLOGIN", "ADDUSER", "CHANGETRACKING",
                "CONTAINS", "CONTAINSTABLE", "EVENTDATA", "FILETABLE",
                "FREETEXTTABLE", "FULLTEXTCATALOGPROPERTY", "FULLTEXTSERVICEPROPERTY",
                "IDENTITY", "IDENTITYCOL", "IDENT_CURRENT", "IDENT_INCR",
                "IDENT_SEED", "FORMSOF", "SEMANTICSIMILARITYTABLE",
                "SEMANTICKEYPHRASETABLE", "PERMISSIONS", "PWDENCRYPT",
                "PWDCOMPARE", "FORMATMESSAGE", "OPENXML", "PIVOT",
                "READTEXT", "ROWVERSION", "TABLESAMPLE", "TEXTPTR",
                "TEXTSIZE", "UPSERT", "MERGE", "REPLACE", "FUNC", "PROCEDURE",
                "VIEW", "INDEX", "CONSTRAINT", "SCHEMA", "DATABASE", "TABLE"
            };

            var words = StringTool.GetWords(sqlCommand);

            foreach (var word in words)
            {
                if (unsafeKeywords.Contains(word.ToUpper()))
                {
                    return false;
                }
            }

            return true;
        }

    }
}

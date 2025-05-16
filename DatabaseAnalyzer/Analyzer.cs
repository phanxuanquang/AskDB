using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using GeminiDotNET;
using GeminiDotNET.Helpers;
using Helper;
using Models.Enums;
using System.Data;
using System.Globalization;
using System.Text;

namespace DatabaseAnalyzer
{
    public static class Analyzer
    {
        public const short MaxTotalTables = 500;
        public const short MaxTotalColumns = 10000;
        public static List<Table> SelectedTables = [];
        public static DatabaseExtractor DbExtractor;
        private static string SampleData = string.Empty;
        private static Generator _generator;
        private static ApiRequestBuilder _apiRequestBuilder = new ApiRequestBuilder()
                .WithDefaultGenerationConfig()
                .DisableAllSafetySettings();

        public static async Task<SqlCommander> GetSql(string question)
        {
            var databaseType = DbExtractor.DatabaseType.ToString();
            var intstruction = $@"You are a highly skilled Database Administrator with over 20 years of experience working with {databaseType} databases in large-scale enterprise systems.
I am a beginner with no knowledge of SQL and need your help to translate plain-language queries into SQL queries for my {databaseType} database.
I will provide the structure of my database, some sample data, and a query in natural language. Your job is to analyze the query, map it to the database schema, and return the corresponding SQL query if it’s possible.

### Instructions for Generating the Response:
1. **Output**: If the input query is valid and can be converted into an SQL query for the {databaseType} database, return the corresponding SQL query. 
   - If the query cannot be converted due to an error, invalid data, or lack of context in the database schema, respond with a clear and explanation.
   - The SQL query should be syntactically correct and adhere to the conventions and features of the {databaseType} database.
2. **IsSql**: 
   - Set this field to `true` if ""Output"" contains a valid SQL query.
   - Set it to `false` if ""Output"" is an explanation or if the input is invalid.

### Example 1: Valid Query
- User’s input: ""List all available products""
- Response:

```json
{{
    ""Output"": ""SELECT * FROM Products WHERE IsAvailable = 1"",
    ""IsSql"": true
}}
```

### Example 2: Invalid Query
- User’s input: ""Show the best-selling product of all time""
- Response:

```json
{{
    ""Output"": ""Invalid request: The database schema does not include sales data necessary to determine best-selling products."",
    ""IsSql"": false
}}
```

### Additional Guidelines ###
1. **Accurate SQL Conversion**: Ensure the generated SQL query is accurate and respects the database schema. Only use tables and columns provided in the database structure.
2. **Database-Specific Syntax**: Be aware of {databaseType}-specific SQL syntax (e.g., T-SQL for SQL Server, MySQL syntax) and adapt the query accordingly.
3. **Clarify Ambiguities**: If the input query is unclear or requires assumptions to be made (e.g., missing table relationships or unclear filtering criteria), respond with description of the ambiguity.
4. **No Guessing**: If the input does not match the schema or is too vague to infer a valid query, be specific about why the query cannot be constructed.
5. **Explain Invalid Queries**: When returning an invalid response, provide a short but clear explanation. Example: ""The query refers to a non-existent column."", or ""There is no table for 'products' in the schema."".
6. **Only Relevant Data**: Focus only on the tables and fields provided in the schema and sample data. Do not make assumptions about data outside of what is presented.

### **Database Schema**:

```sql
{TablesAsString(SelectedTables)}
```

### **Sample Data**:

```sql
{SampleData}
```";

            var apiRequest = _apiRequestBuilder
                .WithSystemInstruction(intstruction)
                .WithPrompt(question)
                .WithDefaultGenerationConfig(0.4F)
                .WithResponseSchema(new
                {
                    type = "object",
                    properties = new
                    {
                        Output = new
                        {
                            type = "string"
                        },
                        IsSql = new
                        {
                            type = "boolean"
                        },
                    },
                    required = new List<string>
                    {
                        "Output",
                        "IsSql"
                    }
                })
                .Build();

            _generator = new Generator(Cache.ApiKey);

            var response = await _generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash);

            return JsonHelper.AsObject<SqlCommander>(response.Content);
        }

        public static async Task<List<string>?> GetSuggestedQueries(bool useSql, sbyte totalSuggestedQueries = 20)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = DbExtractor.DatabaseType.ToString();
            var englishQuery = !useSql ? $"human language ({CultureInfo.CurrentCulture.EnglishName.Split(' ')[0]})" : databaseType;
            var role = !useSql ? "Senor Data Analyst and Expert Data Scientist" : "Database Administrator";

            promptBuilder.AppendLine($"You are a {role} with over 30 years of experience working with {databaseType} databases on large-scaled projects. ");
            promptBuilder.Append("I am someone who knows nothing about SQL or data analysis, but I want to use my data for the decision making purposes. ");
            promptBuilder.Append($"I will provide you with the database schemas with some sample data based on the provided database schemas. You have to suggest at least {totalSuggestedQueries} common and unique {englishQuery} queries from simple level to complex level based on my database structure. ");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Database Schema:");
            promptBuilder.AppendLine("```sql");
            promptBuilder.AppendLine(TablesAsString(SelectedTables));
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Sample Data:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(SampleData);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Output Requirement:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("The output must be structured as a string array, here is an example for the output:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("```json");
            promptBuilder.AppendLine("[");
            if (useSql)
            {
                promptBuilder.AppendLine("    \"SELECT TOP 100 * FROM Table1 WHERE Condition\",");
                promptBuilder.AppendLine("    \"SELECT COUNT(*) FROM Table2 WHERE Condition\",");
                promptBuilder.AppendLine("    \"SELECT DISTINCT(*) FROM Table3 WHERE Condition OrderBy Id DESC\"");
            }
            else
            {
                promptBuilder.AppendLine("    \"Give me items of the ExampleTableName table\",");
                promptBuilder.AppendLine("    \"How many ExampleTableName items that <some_conditions>\",");
                promptBuilder.AppendLine("    \"I want to know 10 latest item of the ExampleTableName that <some_conditions>\",");
                promptBuilder.AppendLine("    \"Tell me the items of the ExampleTableName that <some_conditions> after <some_date>\"");
            }
            promptBuilder.AppendLine("]");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Your response:");

            try
            {
                var apiRequest = _apiRequestBuilder
                    .WithPrompt(promptBuilder.ToString())
                    .Build();

                _generator = new Generator(Cache.ApiKey);
                var response = await _generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash_Lite);

                return JsonHelper.AsObject<List<string>>(response.Content);
            }
            catch
            {
                return null;
            }
        }
        public static async Task<string> GetQuickInsight(string query, DataTable dataTable)
        {
            var instructionBuilder = new StringBuilder();
            instructionBuilder.AppendLine(await Extractor.ReadFile("Instructions/Data Analysis.md"));
            instructionBuilder.AppendLine();
            instructionBuilder.AppendLine();
            instructionBuilder.AppendLine("### **Database Schema**:");
            instructionBuilder.AppendLine();
            instructionBuilder.AppendLine("```sql");
            instructionBuilder.AppendLine(TablesAsString(SelectedTables));
            instructionBuilder.AppendLine("```");

            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine("#### **Query to analyze:**");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"**{query}** ");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("#### **Data Source:**");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(DataAsString(dataTable));

            var apiRequest = _apiRequestBuilder
                .WithSystemInstruction(instructionBuilder.ToString())
                .WithPrompt(promptBuilder.ToString())
                .Build();

            var response = await _generator.GenerateContentAsync(apiRequest, ModelVersion.Gemini_20_Flash);

            return StringTool.AsPlainText(response.Content);
        }

        #region Helpers
        public static async Task ExtractSampleData(short rowsPerTable)
        {
            try
            {
                var sb = new StringBuilder();

                foreach (var table in SelectedTables)
                {
                    var query = DbExtractor.DatabaseType == DatabaseType.SqlServer
                        ? $"SELECT TOP {rowsPerTable} * FROM {table.Name} ORDER BY {table.Columns[0].Name} DESC"
                        : $"SELECT * FROM {table.Name} ORDER BY {table.Columns[0].Name} DESC LIMIT {rowsPerTable}";

                    var exampleDataTable = await DbExtractor.Execute(query);

                    if (exampleDataTable == null || exampleDataTable.Columns.Count == 0 || exampleDataTable.Rows.Count == 0)
                    {
                        continue;
                    }

                    sb.AppendLine($"#### {table.Name}:");
                    sb.AppendLine();

                    foreach (DataColumn column in exampleDataTable.Columns)
                    {
                        sb.Append($"| {column.ColumnName} ");
                    }
                    sb.AppendLine("|");

                    foreach (DataColumn column in exampleDataTable.Columns)
                    {
                        sb.Append("| --- ");
                    }
                    sb.AppendLine("|");

                    foreach (DataRow row in exampleDataTable.Rows)
                    {
                        foreach (DataColumn column in exampleDataTable.Columns)
                        {
                            object value = row[column];
                            sb.Append("| ");

                            if (value == DBNull.Value)
                            {
                                sb.Append("NULL");
                            }
                            else if (value is string || value is DateTime)
                            {
                                sb.Append(value.ToString().Replace("|", "\\|"));
                            }
                            else
                            {
                                sb.Append(value);
                            }

                            sb.Append(" ");
                        }
                        sb.AppendLine("|");
                    }

                    sb.AppendLine();
                }

                SampleData = sb.ToString().Trim();
            }
            catch
            {
                SampleData = string.Empty;
            }
        }

        public static string TablesAsString(List<Table> tables)
        {
            var schemas = tables.Select(d => d.ToString()).ToList();
            return string.Join(string.Empty, schemas).Trim();
        }
        public static string DataAsString(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
                return string.Empty;

            var markdownBuilder = new StringBuilder();

            for (int i = 0; i < table.Columns.Count; i++)
            {
                markdownBuilder.Append("| " + table.Columns[i].ColumnName + " ");
            }
            markdownBuilder.AppendLine("|");

            for (int i = 0; i < table.Columns.Count; i++)
            {
                markdownBuilder.Append("| --- ");
            }
            markdownBuilder.AppendLine("|");

            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    markdownBuilder.Append("| " + row[i].ToString() + " ");
                }
                markdownBuilder.AppendLine("|");
            }

            return markdownBuilder.ToString();
        }
        public static bool IsSqlSafe(string sqlCommand)
        {
            List<string> unsafeKeywords =
            [
                "INSERT INTO", "UPDATE", "DELETE", "ALTER", "CREATE", "DROP", "TRUNCATE", "MERGE", "REPLACE", "ADD",
                "MODIFY", "RENAME", "GRANT", "REVOKE", "COMMIT", "ROLLBACK", "SAVEPOINT", "SET", "LOCK",
                "UNLOCK", "EXPLAIN", "ANALYZE", "OPTIMIZE", "CASCADE", "REFERENCES", "REINDEX", "VACUUM",
                "ENABLE", "DISABLE", "ATTACH", "DETACH", "REPAIR", "REBUILD", "INITIATE", "EXTEND", "SHRINK", "TRANSFER",
                "DISTRIBUTE", "ARCHIVE", "PARTITION", "ADD CONSTRAINT", "DROP CONSTRAINT", "RENAME COLUMN", "ALTER COLUMN",
                "SET DEFAULT", "UNSET DEFAULT", "CONVERT TO", "ALTER INDEX", "CREATE TABLE", "CREATE INDEX", "CREATE VIEW",
                "CREATE PROCEDURE", "CREATE FUNCTION", "CREATE TRIGGER", "CREATE SEQUENCE", "ALTER TABLE", "ALTER VIEW",
                "ALTER PROCEDURE", "ALTER FUNCTION", "ALTER TRIGGER", "ALTER SEQUENCE", "DROP TABLE", "DROP INDEX", "DROP VIEW",
                "DROP PROCEDURE", "DROP FUNCTION", "DROP TRIGGER", "DROP SEQUENCE", "TRUNCATE TABLE", "SET IDENTITY_INSERT",
                "RENAME TABLE", "RENAME INDEX", "RENAME VIEW", "RENAME PROCEDURE", "RENAME FUNCTION", "RENAME TRIGGER",
                "RENAME SEQUENCE", "SET TRANSACTION ISOLATION LEVEL", "BEGIN TRANSACTION", "END TRANSACTION", "BEGIN WORK",
                "END WORK", "CREATE SCHEMA", "DROP SCHEMA", "ALTER SCHEMA", "CREATE USER", "DROP USER", "ALTER USER",
                "CREATE ROLE", "DROP ROLE", "ALTER ROLE", "REVOKE ALL PRIVILEGES", "GRANT ALL PRIVILEGES", "DENY", "REVOKE",
                "CHECK CONSTRAINT", "DISABLE TRIGGER", "ENABLE TRIGGER"
            ];

            return !unsafeKeywords.Exists(keyword => sqlCommand.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        #endregion
    }
}
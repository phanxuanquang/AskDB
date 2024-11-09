using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using GenAI;
using Helper;
using Newtonsoft.Json;
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

        public static async Task<SqlCommander> GetSql(string question)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = DbExtractor.DatabaseType.ToString();

            promptBuilder.AppendLine($"You are a senior Database Administrator with over 20 years of experience working with {databaseType} databases on large-scale enterprise projects. ");
            promptBuilder.Append($"Your goal is to help a beginner, who has no knowledge of SQL, convert their plain-language queries into precise SQL queries for the {databaseType} database. ");
            promptBuilder.Append("I will provide the table schemas of the database, some sample data, and a question written in natural language. Your task is to interpret the question, understand the intent, and return the correct SQL query if it is possible. ");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Your response must include two parts as follows:");
            promptBuilder.AppendLine($"- **Output:** This is your response to my input. Return the SQL query if the plain-language query is relevant to the the provided database schema and can be converted to a valid SQL query for the {databaseType} database. If my input cannot be converted into a {databaseType} query or it is not relevant to the provided table schemas, please respond that my request is invalid with a short explanation. ");
            promptBuilder.AppendLine("- **IsSql:** `True` if Output contains a **valid** SQL query, `False` if Output is an explanation.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("One more important thing is that your response should be structured in the JSON format, matching the following C# class structure:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("```cs");
            promptBuilder.AppendLine("class SqlCommander");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("    string Output;");
            promptBuilder.AppendLine("    bool IsSql;");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("To help you understand my command and do the task more effectively, here are some examples:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("User’s input: \"List all available products\"");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Your response:");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("    \"Output\" : \"SELECT * FROM Products WHERE IsAvailable = 1\",");
            promptBuilder.AppendLine("    \"IsSql\" : true");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("User’s input: \"Show the best-selling product of all time\"");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Your Response:");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("    \"Output\" : \"The database schema does not contain sales data needed to determine best-selling products.\",");
            promptBuilder.AppendLine("    \"IsSql\" : false");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Additional Instructions:");
            promptBuilder.AppendLine("1. Be strict with the structure: The output must exactly match the structure of the given C# class SqlCommander.");
            promptBuilder.AppendLine("2. Focus on accuracy: Carefully analyze the provided table schemas and sample data to determine if the question can be answered.");
            promptBuilder.AppendLine($"3. Use {databaseType}-specific SQL syntax to ensure compatibility with the database type.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Here are the tables in my database with their schemas and their relationship:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("```sql");
            promptBuilder.AppendLine(TablesAsString(SelectedTables));
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine("### Sample data for context:");
            promptBuilder.AppendLine("```sql");
            promptBuilder.AppendLine(SampleData);
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("User's query:");
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine(question);
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Your response:");

            var response = await Generator.GenerateContent(Generator.ApiKey, promptBuilder.ToString(), true, CreativityLevel.Medium, GenerativeModel.Gemini_15_Flash);
            return JsonConvert.DeserializeObject<SqlCommander>(response);
        }

        public static async Task<List<string>?> GetSuggestedQueries(bool useSql, sbyte totalSuggestedQueries = 20)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = DbExtractor.DatabaseType.ToString();
            var englishQuery = !useSql ? $"human language ({CultureInfo.CurrentCulture.EnglishName.Split(' ')[0]})" : databaseType;
            var role = !useSql ? "Senor Data Analyst and Expert Data Scientist" : "Database Administrator";

            promptBuilder.AppendLine($"You are a {role} with over 30 years of experience working with {databaseType} databases on large-scaled projects. ");
            promptBuilder.Append("I am a CEO who knows nothing about SQL or data analysis, but I want to use my data for the decision making purposes. ");
            promptBuilder.Append($"I will provide you with the table structure of my database with some sample data. You have to suggest at least {totalSuggestedQueries} common and unique {englishQuery} queries from simple level to complex level based on my database structure. ");
            promptBuilder.Append("Your response must be a `List<string>` in C# programming language.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("In order to help you understand my command and do the task more effectively, here is an example for your response:");
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
            promptBuilder.AppendLine("Here are the tables in my database with their schemas and their relationship:");
            promptBuilder.AppendLine("```sql");
            promptBuilder.AppendLine(TablesAsString(SelectedTables));
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("This is the some sample data from my database:");
            promptBuilder.AppendLine("```sql");
            promptBuilder.AppendLine(SampleData);
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Your response:");

            try
            {
                return await Generator.GenerateContentAsArray(Generator.ApiKey, promptBuilder.ToString(), CreativityLevel.Medium, GenerativeModel.Gemini_15_Flash);
            }
            catch
            {
                return null;
            }
        }
        public static async Task<string> GetQuickInsight(string query, DataTable dataTable)
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine("You are a Senior Data Analyst and Data Scientist with over 20 years of experience providing insights for large-scale business projects. ");
            promptBuilder.Append("I am a CEO seeking quick, impactful insights based on the data I provide to support strategic decision-making. ");
            promptBuilder.Append("I will provide a query (in SQL or natural language) and the related data. Please analyze the data comprehensively and summarize your insights with clear, practical recommendations. ");
            promptBuilder.Append("Your response should be non-technical, concise (under 150 words), in a single paragraph, and organized in an easy-to-understand format.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Key Objectives:");
            promptBuilder.AppendLine("Your analysis should focus on extracting meaningful insights that address the following:");
            promptBuilder.AppendLine("- **Trends and Patterns**: Identify any significant or recurring trends, behaviors, or shifts in the data that may indicate growth, decline, or seasonal changes.");
            promptBuilder.AppendLine("- **Anomalies and Deviations**: Point out any anomalies or deviations from expected patterns, explaining their potential causes or impact.");
            promptBuilder.AppendLine("- **Opportunities and Risks**: Identify areas for growth, potential challenges, or risks in the data, and suggest how these may affect the company's goals.");
            promptBuilder.AppendLine("- **Strategic Implications**: Explain how these insights could guide high-level decision-making, focusing on areas like revenue growth, customer retention, or operational efficiency.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Specific Instructions for the Analysis:");
            promptBuilder.AppendLine("1. **Data Trends and Summary**: Briefly summarize any key patterns or recurring trends in the data. For example, if the data reveals a steady increase in customer engagement, highlight this trend with potential reasons.");
            promptBuilder.AppendLine("2. **Impactful Metrics and KPIs**: Focus on metrics most relevant to decision-making, such as revenue, conversion rate, or customer satisfaction. Emphasize any notable changes in these metrics.");
            promptBuilder.AppendLine("3. **Anomalies and Deviations**: Identify and explain any outliers or anomalies in the data. Provide context on what could be causing these deviations and any associated risks or opportunities.");
            promptBuilder.AppendLine("4. **Business Relevance and Recommendations**: Conclude by explaining how the identified trends or patterns relate to the company’s strategic goals. Offer clear recommendations if applicable.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Formatting Guidelines for the Insight:");
            promptBuilder.AppendLine("- Keep the insight concise and under 150 words, formatted in a single paragraph.");
            promptBuilder.AppendLine("- Avoid technical jargon; use simple language accessible to non-technical readers.");
            promptBuilder.AppendLine("- Summarize key findings at the start, followed by any relevant details or explanations, and conclude with any strategic recommendations.");
            promptBuilder.AppendLine("- Structure the paragraph in a clear, logical order: first trends, then anomalies, followed by business relevance, and finally recommendations.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Here is the database schema for your reference, including column names, data types, and relationships for each table:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("```sql");
            promptBuilder.AppendLine(TablesAsString(SelectedTables));
            promptBuilder.AppendLine("```");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"The query I am interested in is: **{query}**");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Here is the data table that is used for the data analysis:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(DataAsString(dataTable));
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### Your insight from the data analysis:");

            var result = await Generator.GenerateContent(Generator.ApiKey, promptBuilder.ToString(), false, CreativityLevel.High, GenerativeModel.Gemini_15_Flash);
            return StringTool.AsPlainText(result);
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

                    foreach (DataRow row in exampleDataTable.Rows)
                    {
                        sb.AppendFormat("INSERT INTO {0} (", table.Name);

                        for (int i = 0; i < exampleDataTable.Columns.Count; i++)
                        {
                            sb.Append(exampleDataTable.Columns[i].ColumnName);
                            if (i < exampleDataTable.Columns.Count - 1)
                            {
                                sb.Append(", ");
                            }
                        }

                        sb.Append(") VALUES (");

                        for (int i = 0; i < exampleDataTable.Columns.Count; i++)
                        {
                            object value = row[i];

                            if (value == DBNull.Value)
                            {
                                sb.Append("NULL");
                            }
                            else if (value is string || value is DateTime)
                            {
                                sb.AppendFormat("'{0}'", value.ToString().Replace("'", "''"));
                            }
                            else
                            {
                                sb.Append(value);
                            }

                            if (i < exampleDataTable.Columns.Count - 1)
                            {
                                sb.Append(", ");
                            }
                        }

                        sb.Append(");\n");
                    }
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
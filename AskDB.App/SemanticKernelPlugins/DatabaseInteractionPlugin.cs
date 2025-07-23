using AskDB.App.Pages;
using AskDB.Commons.Extensions;
using DatabaseInteractor.Services;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AskDB.App.SemanticKernelPlugins
{
    public class DatabaseInteractionPlugin(DatabaseInteractionService databaseInteractionService, ChatWithDatabase chatWithDatabasePage)
    {
        [KernelFunction]
        [Description(@"Run a safe, read-only SQL query (e.g., `SELECT`) and return the result as a Markdown table.

**Use when:** You want to explore, inspect, analyze, or validate data without modifying it.

**Best for:**
- Previewing data before update/delete
- Exploring structure/content of tables
- Building query context for downstream tools 
- Investigating data issues, trends, or assumptions
- Discovering join keys or value ranges

**Best practices:**
- Use early to validate ambiguous user input
- Check results before risky operations
- Explore smartly (e.g., sample a few rows, inspect NULLs)
- Combine with: `GetTableStructure`, `SearchTablesByName` for deeper insights

**Tool Chaining**:
- Use *after* `GetTableStructureDetail` to ensure your query uses correct column names.
- Use *before* `ExecuteNonQuery` to preview the rows that will be affected by an `UPDATE` or `DELETE` command.")]
        public async Task<string> ExecuteQuery(
            [Description("A valid SQL SELECT or read-only query to run against the database.")]
            string sqlQuery)
        {

            try
            {
                var dataTable = await databaseInteractionService.ExecuteQueryAsync(sqlQuery!);
                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    chatWithDatabasePage.SetAgentMessage($"Let me execute this query to check the data:\n\n```sql\n{sqlQuery}\n```", dataTable);
                    return dataTable.ToMarkdown();
                }
                else
                {
                    var noDataMessage = "No data found for the query.";
                    chatWithDatabasePage.SetAgentMessage(noDataMessage);
                    return noDataMessage;
                }
            }
            catch (Exception ex)
            {
                chatWithDatabasePage.SetAgentMessage($"Error while executing the query:\n\n```sql\n{sqlQuery}\n```\n\n**Error:**\n\n```console\n{ex.Message}\n```");
                return $"**Error:** {ex.Message}";
            }
        }

        [KernelFunction]
        [Description(@"Execute SQL commands that change data or schema (e.g., `INSERT`, `UPDATE`, `DELETE`, `CREATE`, `DROP`, etc.).

**Use when:** You need to perform a confirmed modification to the database.

**Best for:**
- Inserting, updating, deleting rows
- Altering tables or schema
- Executing DDL/DML after validation

**Warnings:**
- Do NOT use for SELECT or read-only queries
- Validate schema using `get_table_structure` before writing
- Check data first using `execute_query` to confirm targets

**Combine with:** `ExecuteQuery`, `GetTableStructure` to verify impact")]
        public async Task<string> ExecuteNonQuery(
        [Description("A complete SQL command that changes data or schema. Only execute after user intent is clear.")] string sqlCommand)
        {
            try
            {
                await databaseInteractionService.ExecuteNonQueryAsync(sqlCommand);
                chatWithDatabasePage.SetAgentMessage($"Executed the following command successfully:\n\n```sql\n{sqlCommand}\n```");
                return "Executed successfully";
            }
            catch (Exception ex)
            {
                chatWithDatabasePage.SetAgentMessage($"Error while executing your command.\n\n```console\n{ex.Message}\n```");
                return $"**Error:** {ex.Message}";
            }
        }

        [KernelFunction]
        [Description(@"Get the list of current user's database permissions.

**Use when:** You want to check if an action is allowed, explain permission errors, or plan steps based on what the user can do.

**Best for:**
- Diagnosing permission failures
- Guiding users on allowed actions
- Explaining access limitations")]
        public async Task<List<string>> GetUserPermissions()
        {
            var permissions = await databaseInteractionService.GetUserPermissionsAsync();
            var resultMessage = permissions.Count > 0
                ? $"You have the following permissions: {string.Join(", ", permissions)}"
                : "You have no permissions in this database.";

            chatWithDatabasePage.SetAgentMessage(resultMessage);

            return permissions;
        }

        [KernelFunction]
        [Description(@"Searches for relevant table names based on a keyword. Returns a list of matching table names. If the keyword is empty, it returns all accessible tables.
This is the primary tool for discovery.

**Use when:** You don’t know the exact table name, suspect a typo, or need to explore what's available.

**Best for:**
- Discovering correct table names
- Handling partial or ambiguous user input
- Preparing for use of: `get_table_structure`, `generate_select_query`

**Best practices:**
- Always use if table name is vague, misspelled, or unknown
- Prompt user to choose if multiple matches returned
- Supports schema-qualified results

**Tool Chaining**:
- Call this **first** when the user's request is ambiguous, mentions a table name vaguely, or when you need to find the correct, fully-qualified table name (e.g., `dbo.Customers`).
- The output of this function is the ideal input for `GetTableStructureDetail`.")]

        public async Task<List<string>> SearchTablesByName(
        [Description("Keyword as the hint to search table names. Empty string returns all tables with schema.")]
        string? keyword)
        {
            try
            {
                var tableNames = await databaseInteractionService.SearchTablesByNameAsync(keyword);
                var resultMessage = tableNames.Count > 0
                    ? $"Found {tableNames.Count} tables matching `{keyword}`: {string.Join(", ", tableNames.Select(name => $"`{name}`"))}"
                    : $"No tables found matching `{keyword}`.";

                chatWithDatabasePage.SetAgentMessage(resultMessage);

                return tableNames;
            }
            catch (Exception ex)
            {
                chatWithDatabasePage.SetAgentMessage($"Error while searching for tables with keyword `{keyword}`:\n\n**Error:**\n\n```console\n{ex.Message}\n```");
                return [$"**Error:** {ex.Message}"];
            }
        }

        [KernelFunction]
        [Description(@"Get detailed schema info of a table: columns, types, nullability, keys, constraints, references.

**Use when:** You need to understand table structure to build valid SQL or avoid runtime errors.

**Best for:**
- Writing correct SELECT/UPDATE/INSERT/DELETE queries
- Building JOINs or WHERE conditions
- Validating field types and constraints
- Understanding table relationships

**Combine with:**
- `search_tables_by_name` to resolve table/schema names
- `execute_query` to preview values
- `generate_*_query` to ensure compatibility

**Best practices:**
- Always use before generating SQL
- Avoid referencing columns blindly
- Validate schema if user input is unclear")]
        public async Task<string> GetTableStructureDetail(
            [Description("Exact table name (case-sensitive). Required.")]
            string table,
            [Description("Schema name if applicable (for SQL Server/PostgreSQL). Leave blank or null otherwise.")]
            string? schema = "")
        {
            try
            {
                var tableStructure = await databaseInteractionService.GetTableStructureDetailAsync(schema, table);
                if (tableStructure.Rows.Count > 0)
                {
                    if (!string.IsNullOrEmpty(schema))
                    {
                        chatWithDatabasePage.SetAgentMessage($"Let me check the structure of the `{schema}.{table}` table", tableStructure);
                    }
                    else
                    {
                        chatWithDatabasePage.SetAgentMessage($"Let me check the structure of the `{table}` table", tableStructure);
                    }
                    return tableStructure.ToMarkdown();
                }
                else
                {
                    var notFoundMessage = $"No structure found for table `{schema}.{table}`.";
                    chatWithDatabasePage.SetAgentMessage(notFoundMessage);
                    return notFoundMessage;
                }
            }
            catch (Exception ex)
            {
                chatWithDatabasePage.SetAgentMessage($"Error while retrieving structure for table `{table}`:\n\n**Error:**\n\n```console\n{ex.Message}\n```");
                return $"**Error:** {ex.Message}";
            }
        }
    }
}

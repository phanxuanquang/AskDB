using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;

namespace DatabaseInteractor.Services
{
    public static class FunctionCallingManager
    {
        public static readonly FunctionDeclaration ExecuteQueryAsyncFunction = new()
        {
            Name = "ExecuteQueryAsync",
            Description = @"
This function is used **EXCLUSIVELY** for executing SQL queries or SQL cripts that are intended to retrieve data or inspect the database state WITHOUT making any changes. 
This primarily includes SELECT statements.
**DO NOT** use this function for `INSERT`, `UPDATE`, `DELETE`, `CREATE`, `ALTER`, `DROP`, `TRUNCATE`, or any other SQL command that modifies data or schema. For such operations, use `ExecuteNonQueryAsyncFunction` function instead.
The result will be a dataset (e.g., a list of rows and columns).",
            Parameters = new Parameters
            {
                Properties = new
                {
                    sqlQuery = new
                    {
                        type = "string",
                        description = "The complete, syntactically correct SQL query (typically a `SELECT` statement or a read-only system procedure call) to be executed."
                    }
                },
                Required = ["sqlQuery"]
            }
        };

        public static readonly FunctionDeclaration ExecuteNonQueryAsyncFunction = new()
        {
            Name = "ExecuteNonQueryAsync",
            Description = @"
This function is used **EXCLUSIVELY** for executing SQL commands or SQL scripts that modify the database data or schema.
This includes, but is not limited to:
- INSERT statements: To add new records to a table.
- UPDATE statements: To modify existing records in a table.
- DELETE statements: To remove records from a table.
- CREATE statements: To create new database objects (e.g., tables, views, indexes).
- ALTER statements: To modify the structure of existing database objects.
- DROP statements: To remove database objects.
- TRUNCATE TABLE statements: To remove all rows from a table (faster than DELETE without WHERE, but less recoverable and doesn't fire triggers).
Any other DML (Data Manipulation Language) or DDL (Data Definition Language) command that changes the database state but does not primarily return a data result set.
This function typically returns the number of rows affected, not a dataset.
CRITICAL SAFETY NOTE: Always ensure any data modification (INSERT, UPDATE, DELETE) or destructive (DROP, TRUNCATE) operations executed via this function have been explicitly planned and confirmed by the user according to the Core Problem-Solving & Confidence Protocol.
**DO NOT** use this function for SELECT statements or other read-only queries that are expected to return data; use ExecuteQueryAsyncFunction for those.",
            Parameters = new Parameters
            {
                Properties = new
                {
                    sqlQuery = new
                    {
                        type = "string",
                        description = "The complete, syntactically correct SQL command or SQL scripts (e.g., INSERT, UPDATE, DELETE, CREATE, ALTER) to be executed. Ensure the query is specific to the user-confirmed action plan."
                    }
                },
                Required = ["sqlQuery"]
            }
        };

        public static List<FunctionDeclaration> FunctionDeclarations { get; } =
        [
            ExecuteQueryAsyncFunction,
            ExecuteNonQueryAsyncFunction
        ];

        public static void RegisterFunction(string name, string description, Parameters? parameters = null)
        {

            FunctionDeclarations.Add(new FunctionDeclaration
            {
                Name = name,
                Description = description,
                Parameters = parameters
            });
        }
    }
}

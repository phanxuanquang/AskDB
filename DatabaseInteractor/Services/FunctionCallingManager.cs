using GeminiDotNET.ApiModels.ApiRequest.Configurations.Tools.FunctionCalling;

namespace DatabaseInteractor.Services
{
    public static class FunctionCallingManager
    {
        public static readonly FunctionDeclaration ExecuteQueryAsyncFunction = new()
        {
            Name = "ExecuteQueryAsync",
            Description = "Execute a SQL query and return the result set. This is used for data retrieval operations (SELECT statements). You can also use it for schema introspection (e.g., checking table structures, column names, etc.), for data analysis tasks, for inspecting data, and for any other read-only operations.",
            Parameters = new Parameters
            {
                Properties = new
                {
                    sqlQuery = new
                    {
                        type = "string",
                        description = "The SQL query to execute. It can be a SELECT statement or any other SQL command that returns a result set."
                    }
                }
            }
        };

        public static readonly FunctionDeclaration ExecuteNonQueryAsyncFunction = new()
        {
            Name = "ExecuteNonQueryAsync",
            Description = "Execute a SQL query that performs data-changing or schema-altering operations (INSERT, UPDATE, DELETE, CREATE, ALTER, etc.). This is used for any operation that modifies the database state, such as inserting new records, updating existing ones, or creating new tables. You can also use it for any other write-only operations.",
            Parameters = new Parameters
            {
                Properties = new
                {
                    sqlQuery = new
                    {
                        type = "string",
                        description = "The SQL query to execute. It can be an INSERT, UPDATE, DELETE, or any other SQL command that does not return a result set."
                    }
                }
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

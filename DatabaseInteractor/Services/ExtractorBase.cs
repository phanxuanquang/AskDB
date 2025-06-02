using AskDB.Commons.Enums;
using DatabaseInteractor.Function_Callings.Attributes;
using System.Data;

namespace DatabaseInteractor.Services
{
    public abstract class ExtractorBase(string connectionString)
    {
        protected string ConnectionString = connectionString;
        public DatabaseType DatabaseType { get; protected set; }

        [FunctionDeclaration("execute_query", @"Execute a SQL query and return the result as a dataset (e.g., a list of rows and columns).
Use this function **EXCLUSIVELY** for executing SQL queries or SQL cripts that are intended to retrieve data or inspect the database state **WITHOUT** making any changes. 
This primarily includes SELECT statements.
**DO NOT** use this function for `INSERT`, `UPDATE`, `DELETE`, `CREATE`, `ALTER`, `DROP`, `TRUNCATE`, or any other SQL command that modifies data or schema.
Prefer to use `get_table_structure` function to check and understand the structure of the relevant tables before executing if you are unsure about the query or its impact.")]
        public abstract Task<DataTable> ExecuteQueryAsync(string sqlQuery);

        [FunctionDeclaration("execute_non_query", @"Execute a SQL query that does not return any result (e.g., INSERT, UPDATE, DELETE).
Use this function **EXCLUSIVELY** for executing SQL commands or SQL scripts that modify the database data or schema.

This includes, but is not limited to:
- INSERT statements: To add new records to a table.
- UPDATE statements: To modify existing records in a table.
- DELETE statements: To remove records from a table.
- CREATE statements: To create new database objects (e.g., tables, views, indexes).
- ALTER statements: To modify the structure of existing database objects.
- DROP statements: To remove database objects.
- TRUNCATE TABLE statements: To remove all rows from a table (faster than DELETE without WHERE, but less recoverable and doesn't fire triggers).

**CRITICAL SAFETY NOTES:** 
- Always ensure any data modification (INSERT, UPDATE, DELETE) or destructive (DROP, TRUNCATE) operations executed via this function have been explicitly planned and confirmed by the user according to the Core Problem-Solving & Confidence Protocol.
- Prefer to use `get_table_structure` function to check and understand the structure of the relevant tables before executing if you are unsure about the query or its impact.
- Prefer to use `execute_query` function to check the data of the impacted tables before executing if you are unsure about the query or its impact.
- **DO NOT** use this function for SELECT statements or other read-only queries that are expected to return data")]
        public abstract Task ExecuteNonQueryAsync(string sqlQuery);

        [FunctionDeclaration("get_user_permissions", @"Get the list of permissions for the current user.
Use this function **EXCLUSIVELY** to retrieve a list of the database permissions currently granted to the application's user session.

This information can be crucial for:
- Understanding why certain operations might be failing due to insufficient privileges.
- Assessing if a requested action is permissible before attempting it.
- Informing the user about their current capabilities within the database if they inquire or if it's relevant to a problem.")]
        public abstract Task<List<string>> GetUserPermissionsAsync();

        [FunctionDeclaration("search_schemas_by_name", @"Search for schemas by name and returns a list of schema names that contain the keyword. If the keyword is an empty string, all schema names will be returned.
Use this function to retrieve a list of available schema names in the current database.

This function is useful when:
- The user refers to a table but doesn't specify a schema, and you need to find potential schemas.
- You need to present a list of schemas to the user for selection.
- You are trying to fully qualify a table name (schema.table) and need to confirm schema existence.
- The user explicitly asks to list schemas.")]
        public abstract Task<List<string>> SearchSchemasByNameAsync(string? keyword);

        [FunctionDeclaration("search_tables_by_name", @"Search for tables by name within a specific schema. Returns a list of table names that contain the keyword. If the keyword is an empty string, all table names will be returned.

Use this function to retrieve a list of table names within a specific schema in the current database. 

This tool is **CRITICAL** for:
- Identifying available tables in a schema context when the user does not specify a table name.
- Assisting the user in selecting a table for further operations (e.g., querying, updating).
- Ensuring that the user is aware of the tables available in a schema before attempting to access or modify them.
- Filtering tables based on a keyword allows for more targeted searches, especially in databases with many tables.

If the user does not specify a schema, you should use the default schema of the database (usually `dbo` for SQL Server, `public` for PostgreSQL, etc.).
If unsure about the schema, consider using `search_schemas_by_name` function first with your defined parameters, or asking the user for the clarification.")]
        public abstract Task<List<string>> SearchTablesByNameAsync(string? schema, string? keyword);

        [FunctionDeclaration("get_table_structure", @"Get the structure of a table, including column names and types. Returns a DataTable with the structure information.
Use this function to retrieve structural information for a specific table.
The information returned includes:
- Column names, data types, nullability, constraints, foreign keys, references.
This tool is **CRITICAL** for:
- Formulating accurate and safe SQL queries, especially when constructing WHERE clauses or JOIN conditions.
- Understanding data types before attempting INSERT or UPDATE operations.
- Verifying table and column existence before referencing them in queries.
- Helping the user understand their data structure if they ask.
- Assessing potential impacts of queries (e.g., understanding relationships before a DELETE).
- Understanding potential issues with data integrity before making changes.
If the user does not specify a schema, you should use the default schema of the database (usually `dbo` for SQL Server, `public` for PostgreSQL, etc.).
If unsure about the schema, consider using `search_schemas_by_name` function first with your defined parameters, or asking the user for the clarification.")]
        public abstract Task<DataTable> GetTableStructureDetailAsync(string? schema, string table);

        public abstract Task EnsureDatabaseConnectionAsync();
    }
}

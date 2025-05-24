using DatabaseInteractor.Models.Enums;
using System.Data;

namespace DatabaseInteractor.Services
{
    public abstract class ExtractorBase(string connectionString)
    {
        protected string ConnectionString = connectionString;
        protected DatabaseType DatabaseType;

        /// <summary>
        /// Executes the specified SQL query asynchronously and retrieves the results as a <see cref="DataTable"/>.
        /// </summary>
        /// <remarks>The method executes the query asynchronously, allowing the caller to perform other
        /// operations while waiting for the results. Ensure that the SQL query is properly formatted and that the
        /// database connection is open before calling this method.</remarks>
        /// <param name="sqlQuery">The SQL query to execute. Must be a valid SQL statement.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="DataTable"/> with
        /// the query results.</returns>
        public abstract Task<DataTable> ExecuteQueryAsync(string sqlQuery);

        /// <summary>
        /// Executes a non-query SQL command asynchronously.
        /// </summary>
        /// <remarks>Use this method to execute SQL commands that do not return any result sets. For
        /// example, use it to modify data in a database.</remarks>
        /// <param name="sqlQuery">The SQL query to execute. This must be a valid non-query command, such as an INSERT, UPDATE, or DELETE
        /// statement.</param>
        /// <returns>A task that represents the asynchronous operation. The task completes when the command has been executed.</returns>
        public abstract Task ExecuteNonQueryAsync(string sqlQuery);

        /// <summary>
        /// Retrieves a list of permissions associated with the current user.
        /// </summary>
        /// <remarks>This method must be implemented in a derived class. The implementation should ensure
        /// that the returned permissions  accurately reflect the user's current access rights.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of strings,  where each
        /// string represents a permission assigned to the user. The list will be empty if the user has no permissions.</returns>
        public abstract Task<List<string>> GetUserPermissionsAsync();

        /// <summary>
        /// Retrieves a list of database schema names that contains the specified keyword ignoring ordinal case.
        /// </summary>
        /// <param name="keyword">An optional keyword to filter the schema names. If <see langword="null"/> or empty, all schema names are
        /// returned.</param>
        /// <returns>A list of schema names that match the specified keyword. The list will be empty if no matching schemas are found.</returns>
        public abstract Task<List<string>> GetDatabaseSchemaNamesAsync(string? keyword);

        /// <summary>
        /// Retrieves schema information for a specified table within a given schema, including table structure, constraints, relationships, primary keys, and foreign keys.
        /// </summary>
        /// <param name="schema">The name of the schema containing the table. Cannot be null or empty.</param>
        /// <param name="table">The name of the table for which to retrieve schema information. Cannot be null or empty.</param>
        /// <returns>The schema information for the specified table.</returns>
        public abstract Task<DataTable> GetSchemaInfoAsync(string schema, string table);
    }
}

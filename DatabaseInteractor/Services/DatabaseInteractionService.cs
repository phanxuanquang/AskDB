using AskDB.Commons.Enums;
using DatabaseInteractor.Factories;
using DatabaseInteractor.Helpers;
using GeminiDotNET.FunctionCallings.Attributes;
using System.Data;
using System.Data.Common;

namespace DatabaseInteractor.Services
{
    public abstract class DatabaseInteractionService(string connectionString)
    {
        public HashSet<string> CachedAllTableNames { get; set; } = [];
        protected string SearchTablesByNameQueryTemplate { get; private set; }
        public string GetTableStructureDetailQueryTemplate { get; private set; }

        public DatabaseType DatabaseType { get; protected set; }
        public string ConnectionString { get; } = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        protected DbConnection GetConnection() => ServiceFactory.CreateConnection(DatabaseType, ConnectionString);
        public async Task EnsureDatabaseConnectionAsync()
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();
        }

        protected async Task<DataTable> ExecuteQueryAsync(DbCommand command)
        {
            await using var connection = GetConnection();
            command.Connection ??= connection;

            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync();

            var dataTable = new DataTable();
            await using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            return dataTable;
        }

        [FunctionDeclaration("execute_query", @"Execute a **read-only SQL query** against the current database and return the result as a structured dataset (rows and columns).
This function is strictly for executing `SELECT` statements or other **non-modifying**, **safe** SQL queries/scripts.

### **Purpose**

Use this function when you need to **read**, **inspect**, or **analyze data** without altering the database state.
This includes:

* Executing SELECT queries to retrieve specific information
* Exploring data patterns
* Building a mental model of the database structure and contents
* Supporting intelligent recommendations or follow-up tool usage

---

### **When to Use**

This function can be used in a wide range of scenarios, including but not limited to:

#### **General Data Exploration**

* Browse sample rows from a table.
* Understand value distributions of a column.
* Examine recent entries.

#### **Analytical Queries**

* Summarize data.
* Aggregate for trends.
* Perform comparisons.

#### **Data Profiling for Other Tools**

* Retrieve column values to suggest filters.
* Detect missing data.
* Identify high-volume tables.

#### **Context Building (Agent-Initiated)**

Use this tool proactively to:

* Build internal **knowledge of table contents** to support more accurate query generation
* Create a **knowledge base** of common values, relationships, or patterns
* Support intelligent **follow-up actions** or **suggestions to the user**
* Confirm whether a certain assumption about the data is true before generating a recommendation
* **Validate or enrich hypotheses** before attempting a multi-step operation
* Prepare input for another function such as `generate_select_query` or `generate_update_query`

#### **User Assistance & Clarification**

* When user input is vague or underspecified (e.g., ""get me data on recent customers""), you can explore tables that might be relevant and validate them
* If the user omits table or column names, use exploratory queries to infer likely sources
* When the user provides partial criteria, use this tool to identify how to map them to actual data structures

#### **Debugging & Investigation**

* Check whether a previous action had any effect
* Confirm the presence or absence of problematic records
* Investigate abnormal trends or suspicious data (e.g., spikes in logs, nulls, outliers)

#### **Complex Workflow Support**

Use as part of a multi-step logic chain:

* Fetch data from one table to join or compare with another
* Test a subquery separately before using it in a larger `generate_query`
* Preview intermediate results for building nested queries

---

### **Tips**

* **Explore before asking**: Run small `SELECT` queries to understand data when the user's request is vague or missing details. When in doubt, or when a user query is ambiguous, consider using `execute_query` **proactively** to explore potential answers, validate assumptions, and prepare smarter responses.
* **Preview before action**: Always check affected rows with a `SELECT` before suggesting `UPDATE` or `DELETE`.
* **Build context**: Query data to understand table relationships, key columns, and typical values before using other tools.
* **Validate assumptions**: Confirm that target data exists and meets conditions before proceeding with logic or tool calls.
* **Use for JOIN discovery**: Look for shared fields (e.g., `UserId`, `OrderId`) across tables to build safe JOINs.
* **Sample rows smartly**: Pull a few rows to clarify structure, content, or to guide other tool outputs (like summaries or suggestions).
* **Explore freely when helpful**: You’re allowed to query the DB to learn, build knowledge, or assist the user better — even without direct user instruction.
* **Avoid surprises**: Don’t assume column names, data types, or content. Check first. 
* **Think ahead**: Use early queries to prepare for more complex operations later.")]
        public async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            await using var connection = GetConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.Connection = connection;

            return await ExecuteQueryAsync(command);
        }

        [FunctionDeclaration("execute_non_query", @"Execute a SQL query that does not return any result (e.g., INSERT, UPDATE, DELETE).
Use this function **EXCLUSIVELY** for executing SQL commands or SQL scripts that modify the database data or schema.

This includes, but is not limited to:
- CREATE statements: To create new database objects (e.g., tables, views, indexes).
- INSERT statements: To add new records to a table.
- UPDATE statements: To modify existing records in a table.
- DELETE statements: To remove records from a table.
- ALTER statements: To modify the structure of existing database objects.
- DROP statements: To remove database objects.
- TRUNCATE TABLE statements: To remove all rows from a table (faster than DELETE without WHERE, but less recoverable and doesn't fire triggers).

**CRITICAL NOTES:** 
- Prefer to use `get_table_structure` function to check and understand the structure of the relevant tables before executing if you are unsure about the query or its impact.
- Prefer to use `execute_query` function to check the data of the impacted tables before executing if you are unsure about the query or its impact.
- **DO NOT** use this function for SELECT statements or other read-only queries that are expected to return data")]
        public async Task ExecuteNonQueryAsync(string sqlCommand)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sqlCommand;
                command.Transaction = transaction;

                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [FunctionDeclaration("get_user_permissions", @"Get the list of permissions for the current user.
Use this function **EXCLUSIVELY** to retrieve a list of the database permissions currently granted to the application's user session.

This information can be crucial for:
- Understanding why certain operations might be failing due to insufficient privileges.
- Assessing if a requested action is permissible before attempting it.
- Informing the user about their current capabilities within the database if they inquire or if it's relevant to a problem.")]
        public abstract Task<List<string>> GetUserPermissionsAsync();

        [FunctionDeclaration("search_tables_by_name", @"Search for tables that may be relevant to the provided keyword by using approximate string matching algorithms (including Levenshtein Distance, Jaro-Winkler Similarity, and N-gram).
If the keyword is an **empty string**, it will return **all available tables** in the current database, otherwise it will return a list of table names (that have the highest similarity with the entered keyword), along with their associated schema names (if applicable).

---

### **Returned information includes:**

* Table name (has highest similarity with the entered keyword).
* Table's associated schema name (if applicable).

---

### **When to call this function:**

This function is **CRITICAL** in the following use cases:

#### **Discovering Table Names**

* The user refers to a table **by an approximate or incomplete name** (e.g., “customer”, “orders”, “log”).
* You are unsure whether a table with a given name actually exists.
* You need to determine the **correct schema-qualified name** (e.g., `dbo.Customers`) before generating SQL.

#### **Schema Exploration**

* The user may asks:

  * “What tables do I have?”
  * “List all tables in the database.”
  * “Do you see a table related to invoices?”
* You need to help the user **explore or browse** the available database tables before they decide what to do next.

#### **Assisting Table Selection**

* The user intends to run a query or retrieve data but hasn’t specified the exact table name.
* You’re building an assistant flow that includes **autocomplete, table pickers, or table previews**.
* You need to present the user with **a list of matching tables** to clarify their intent before proceeding.

#### **Supporting Other Functions**

* Use this function to **identify the correct schema name** before calling functions like:

  * `get_table_structure`
  * `query_table_data`
  * `generate_select_query`
* Especially useful in databases with **multiple schemas**, where table names may repeat (e.g., `sales.Orders`, `archive.Orders`).

#### **Keyword-Based Filtering**

* When the user provides a **partial keyword** (e.g., “log”, “user”, “trans”) and you need to **narrow down the candidate tables**.
* Ideal for large databases where retrieving all tables without filtering is inefficient or overwhelming.

---

### **Best practices:**

* Prefer to call this function when:

  * The user **does not specify a table name** clearly.
  * The user **misspells** or **partially writes** a table name.
  * You are about to call `get_table_structure` or generate SQL for a table, but schema/table name **ambiguity exists**.
* If this function returns **multiple matches**, prompt the user to select or confirm the correct table before continuing.")]
        public abstract Task<List<string>> SearchTablesByNameAsync(string? keyword, int? maxResult = 20000);

        [FunctionDeclaration("get_table_structure", @"Retrieve detailed schema-level metadata of a specific database table, including column names, data types, nullability, constraints, foreign keys, and references.

Use this function to inspect and understand the structure and relational properties of a database table before performing any query or analysis.

---

### **When to call this function:**

This function is **essential** and should be used in the following scenarios:

#### **Query Construction**

* You are about to **generate a SQL SELECT, UPDATE, DELETE, or INSERT query** and need to ensure correctness based on actual column types and constraints.
* You need to **build JOIN conditions** and must know foreign key relationships.
* You are writing `WHERE` clauses and must know **column names, types, and whether they allow NULLs**.
* You need to use functions or comparisons that depend on the **column’s data type** (e.g., `LIKE`, date comparisons, `IS NULL`).

#### **Data Validation & Safety**

* You want to **validate user-provided input** before generating `INSERT` or `UPDATE` statements.
* You want to **avoid referencing non-existent columns or mistyped column names**.
* You need to **check for required fields** (e.g., columns that are `NOT NULL` without defaults).
* You want to **prevent runtime SQL errors** due to incorrect assumptions about schema.

#### **Understanding Relationships**

* You need to know **how this table connects to other tables**, especially before:

  * Performing JOINs.
  * Writing nested SELECTs or correlated subqueries.
  * Deleting rows (to check for ON DELETE CASCADE or foreign key dependencies).

#### Explainability & Debugging

* The user may asks:

  * “What’s in this table?”
  * “What are its fields/columns?”
  * “Which column is the primary key?”
  * “What does this table relate to?”
* You are debugging query errors or verifying schema assumptions.

#### **Impact Analysis**

* You are about to:

  * Delete or update data and need to **check foreign key constraints or cascading rules**.
  * Perform operations that may violate **CHECK constraints** or **data type limitations**.
  * Analyze how changes to one table might affect other tables through **relationships**.

#### **Schema Inference or Exploration**

* You are exploring an unfamiliar database and trying to **understand the data model**.
* You are building an **interactive query assistant**, **AI data agent**, or **SQL generator** that needs reliable schema context.
* You’re generating **documentation** or **metadata views** for the user.

---

### **Schema uncertainty handling:**

If the user doesn’t specify a schema or the schema is unclear:

* Use the `search_tables_by_name` function first to identify available tables and their corresponding schemas.
* If multiple candidates are returned or ambiguity remains, **prompt the user to clarify** which table/schema they meant before calling this function.")]
        public abstract Task<DataTable> GetTableStructureDetailAsync(string? schema, string table);

        protected List<string> SearchTablesFromCachedTableNames(string keyword)
        {
            var searcher = new SimilaritySearchHelper(CachedAllTableNames, 5);
            var levenshteinResults = searcher.LevenshteinSearch(keyword);
            var jaroResults = searcher.JaroWinklerSearch(keyword);
            var ngramResults = searcher.NgramSearch(keyword);

            var results = new HashSet<string>();
            results.UnionWith(levenshteinResults);
            results.UnionWith(jaroResults);
            results.UnionWith(ngramResults);

            return [.. results];
        }

        public async Task InitQueryTemplatesAsync()
        {
            var searchTablesTask = OnlineContentHelper.GeSqlContentAsync(DatabaseType, nameof(SearchTablesByNameAsync));
            var getTableStructureTask = OnlineContentHelper.GeSqlContentAsync(DatabaseType, nameof(GetTableStructureDetailAsync));

            await Task.WhenAll(searchTablesTask, getTableStructureTask);

            SearchTablesByNameQueryTemplate = searchTablesTask.Result;
            GetTableStructureDetailQueryTemplate = getTableStructureTask.Result;
        }    
    }
}

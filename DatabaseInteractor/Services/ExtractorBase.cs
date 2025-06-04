using AskDB.Commons.Enums;
using GeminiDotNET.FunctionCallings.Attributes;
using System.Data;

namespace DatabaseInteractor.Services
{
    public abstract class ExtractorBase(string connectionString)
    {
        protected string ConnectionString = connectionString;
        public DatabaseType DatabaseType { get; protected set; }

        [FunctionDeclaration("execute_query", @"Retrieve a list of database-level and object-level permissions granted to the current user (i.e., the identity under which the current session is connected to the database).

Use this function **exclusively** to check what operations the current user is authorized to perform, both globally and per table/view/procedure.

---

### **When to call this function:**

This function is **critical** in the following situations:

#### **Diagnosing Errors or Failures**

* A query fails with errors like:

  * “Permission denied”
  * “The SELECT/INSERT/UPDATE permission was denied on the object…”
* You suspect that the user is trying to perform an action they are **not authorized** to do.

#### **Proactive Permission Checking**

* Before attempting an operation such as:

  * `INSERT INTO` a table
  * `DELETE FROM` a table
  * Running a `SELECT` with JOINs across multiple tables
  * Executing a stored procedure
* Use this function to ensure that the current user **has the required permissions** and prevent failure in advance.

#### **Explaining Capabilities**

* The user may asks:

  * “What can I do in this database?”
  * “Do I have permission to update this table?”
  * “Why can’t I run this query?”
* You want to provide an **informative explanation** of the user's current access rights.

#### **Security Context Awareness**

* You’re working in a **multi-tenant system**, shared environment, or limited-access user session and need to understand the **boundaries of access**.
* You are trying to assess whether a user can:

  * Create new tables or views.
  * Modify the schema.
  * Drop objects.
  * Execute admin-like commands.

#### **Adaptive Query Generation**

* You want to dynamically **generate SQL** based on the user’s actual capabilities (e.g., only suggest `SELECT` queries if they lack `INSERT`/`UPDATE`).
* You want to provide **limited UI options** or disable actions based on their permission level (e.g., in a SQL assistant or admin tool).

---

### **Important notes:**

* This function only checks permissions **granted to the current session’s database user**.
* It **does not modify** permissions or impersonate other users.
* If the user’s identity is unclear or error messages point to possible permission issues, **call this function first** before attempting further diagnostics.
")]
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

        [FunctionDeclaration("search_tables_by_name", @"Search for tables in the current database by name.
Returns a list of table names along with their associated schema names (if any).

You can provide a **keyword** (partial or full table name) to filter the results. If the keyword is an **empty string**, the function will return **all available tables** in the current database.

---

### **Returned information includes:**

* Table name (exact match or partial match depending on keyword).
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

* Always call this function if:

  * The user **does not specify a table name** clearly.
  * The user **misspells** or **partially writes** a table name.
  * You are about to call `get_table_structure` or generate SQL for a table, but schema/table name **ambiguity exists**.
* If this function returns **multiple matches**, prompt the user to select or confirm the correct table before continuing.")]
        public abstract Task<List<string>> SearchTablesByNameAsync(string? keyword);

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

        public abstract Task EnsureDatabaseConnectionAsync();
    }
}

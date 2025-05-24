using DatabaseInteractor.Models.Enums;
using System.Data;
using System.Data.SqlClient;

namespace DatabaseInteractor.Services.Extractors
{
    public class SqlServerExtractor : ExtractorBase
    {
        public SqlServerExtractor(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.SqlServer;
        }
        public override async Task<DataTable> ExecuteQueryAsync(string sqlQuery)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sqlQuery, connection);
            var dataTable = new DataTable();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        public override async Task ExecuteNonQueryAsync(string sqlQuery)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sqlQuery, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var permissions = new List<string>();
            var query = "SELECT permission_name FROM fn_my_permissions(NULL, 'DATABASE')";

            using (var connection = new SqlConnection(ConnectionString))
            {
                using var command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    permissions.Add(reader.GetString(0));
                }
            }
            return permissions;
        }

        public override async Task<List<string>> GetDatabaseSchemaNamesAsync(string? keyword)
        {
            var schemas = new List<string>();
            var query = "SELECT schema_name FROM information_schema.schemata";
            if (!string.IsNullOrEmpty(keyword))
            {
                query += " WHERE schema_name LIKE @keyword";
            }
            using (var connection = new SqlConnection(ConnectionString))
            {
                using var command = new SqlCommand(query, connection);
                if (!string.IsNullOrEmpty(keyword))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                }

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    schemas.Add(reader.GetString(0));
                }
            }
            return schemas;
        }

        public override async Task<DataTable> GetSchemaInfoAsync(string schema, string table)
        {
            var dataTable = new DataTable("SchemaInfo");

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Query for columns
            var columnsQuery = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
                ORDER BY c.ORDINAL_POSITION";

            // Query for primary keys
            var pkQuery = @"
                SELECT k.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
                    ON t.CONSTRAINT_NAME = k.CONSTRAINT_NAME
                    AND t.TABLE_SCHEMA = k.TABLE_SCHEMA
                    AND t.TABLE_NAME = k.TABLE_NAME
                WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_NAME = @table AND t.CONSTRAINT_TYPE = 'PRIMARY KEY'";

            // Query for foreign keys
            var fkQuery = @"
                SELECT 
                    fk.CONSTRAINT_NAME,
                    kcu.COLUMN_NAME,
                    fkcu.TABLE_SCHEMA AS REFERENCED_SCHEMA,
                    fkcu.TABLE_NAME AS REFERENCED_TABLE,
                    fkcu.COLUMN_NAME AS REFERENCED_COLUMN
                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS fk
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON fk.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    AND fk.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fkcu
                    ON fk.UNIQUE_CONSTRAINT_NAME = fkcu.CONSTRAINT_NAME
                    AND fk.UNIQUE_CONSTRAINT_SCHEMA = fkcu.CONSTRAINT_SCHEMA
                    AND kcu.ORDINAL_POSITION = fkcu.ORDINAL_POSITION
                WHERE kcu.TABLE_SCHEMA = @schema AND kcu.TABLE_NAME = @table";

            // Query for constraints
            var constraintsQuery = @"
                SELECT 
                    tc.CONSTRAINT_NAME,
                    tc.CONSTRAINT_TYPE
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                WHERE tc.TABLE_SCHEMA = @schema AND tc.TABLE_NAME = @table";

            // Prepare result table structure
            dataTable.Columns.Add("ColumnName", typeof(string));
            dataTable.Columns.Add("DataType", typeof(string));
            dataTable.Columns.Add("IsNullable", typeof(string));
            dataTable.Columns.Add("MaxLength", typeof(int));
            dataTable.Columns.Add("DefaultValue", typeof(string));
            dataTable.Columns.Add("IsPrimaryKey", typeof(bool));
            dataTable.Columns.Add("IsForeignKey", typeof(bool));
            dataTable.Columns.Add("ForeignKeyName", typeof(string));
            dataTable.Columns.Add("ReferencedSchema", typeof(string));
            dataTable.Columns.Add("ReferencedTable", typeof(string));
            dataTable.Columns.Add("ReferencedColumn", typeof(string));
            dataTable.Columns.Add("Constraints", typeof(string));

            // Get columns
            var columns = new List<(string Name, string DataType, string IsNullable, int? MaxLength, string DefaultValue)>();
            using (var cmd = new SqlCommand(columnsQuery, connection))
            {
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add((
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        reader.IsDBNull(4) ? null : reader.GetString(4)
                    ));
                }
            }

            // Get primary keys
            var primaryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(pkQuery, connection))
            {
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    primaryKeys.Add(reader.GetString(0));
                }
            }

            // Get foreign keys
            var foreignKeys = new Dictionary<string, (string FKName, string RefSchema, string RefTable, string RefColumn)>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(fkQuery, connection))
            {
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var column = reader.GetString(1);
                    foreignKeys[column] = (
                        reader.GetString(0),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetString(4)
                    );
                }
            }

            // Get constraints
            var constraints = new List<string>();
            using (var cmd = new SqlCommand(constraintsQuery, connection))
            {
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    constraints.Add($"{reader.GetString(0)} ({reader.GetString(1)})");
                }
            }
            var constraintsStr = string.Join("; ", constraints);

            // Compose result
            foreach (var (Name, DataType, IsNullable, MaxLength, DefaultValue) in columns)
            {
                var isPK = primaryKeys.Contains(Name);
                var isFK = foreignKeys.TryGetValue(Name, out var fkInfo);
                dataTable.Rows.Add(
                    Name,
                    DataType,
                    IsNullable,
                    MaxLength ?? (object)DBNull.Value,
                    DefaultValue ?? (object)DBNull.Value,
                    isPK,
                    isFK,
                    isFK ? fkInfo.FKName : null,
                    isFK ? fkInfo.RefSchema : null,
                    isFK ? fkInfo.RefTable : null,
                    isFK ? fkInfo.RefColumn : null,
                    constraintsStr
                );
            }

            return dataTable;
        }
    }
}

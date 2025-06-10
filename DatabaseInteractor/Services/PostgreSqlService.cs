using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using Npgsql;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class PostgreSqlService : DatabaseInteractionService
    {
        public PostgreSqlService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.PostgreSQL;
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            var query = @$"
                SELECT 
                    a.attname AS column_name,
                    pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type,
                    NOT a.attnotnull AS is_nullable,
                    pg_get_expr(ad.adbin, ad.adrelid) AS column_default,
                    CASE
                        WHEN ct.contype = 'p' THEN 'PRIMARY KEY'
                        WHEN ct.contype = 'f' THEN 'FOREIGN KEY'
                        WHEN ct.contype = 'u' THEN 'UNIQUE'
                        WHEN ct.contype = 'c' THEN 'CHECK'
                        ELSE NULL
                    END AS constraint_type,
                    fk_ns.nspname AS foreign_table_schema,
                    fk_cls.relname AS foreign_table_name,
                    fk_att.attname AS foreign_column_name
                FROM 
                    pg_attribute a
                JOIN 
                    pg_class cls ON cls.oid = a.attrelid
                JOIN 
                    pg_namespace ns ON ns.oid = cls.relnamespace
                LEFT JOIN 
                    pg_attrdef ad ON ad.adrelid = a.attrelid AND ad.adnum = a.attnum
                LEFT JOIN 
                    pg_constraint ct ON ct.conrelid = a.attrelid AND a.attnum = ANY (ct.conkey)
                LEFT JOIN 
                    pg_class fk_cls ON fk_cls.oid = ct.confrelid
                LEFT JOIN 
                    pg_namespace fk_ns ON fk_ns.oid = fk_cls.relnamespace
                LEFT JOIN 
                    LATERAL unnest(ct.confkey) WITH ORDINALITY AS fk(attnum, ord) ON ct.contype = 'f'
                LEFT JOIN 
                    LATERAL (
                        SELECT attname 
                        FROM pg_attribute 
                        WHERE attrelid = ct.confrelid 
                        ORDER BY attnum
                        OFFSET fk.attnum - 1 LIMIT 1
                    ) AS fk_att ON true
                WHERE 
                    cls.relname = @table
                    AND ns.nspname = @schema
                    AND a.attnum > 0
                    AND NOT a.attisdropped
                ORDER BY 
                    a.attnum;";

            await using var command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@table", table);
            command.Parameters.AddWithValue("@schema", string.IsNullOrEmpty(schema) ? "public" : schema);
            return await ExecuteQueryAsync(command);
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            var query = "SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = CURRENT_USER";
            var data = await ExecuteQueryAsync(query);
            return data.ToListString();
        }

        public override async Task<List<string>> SearchTablesByNameAsync(string? keyword, int? maxResult = 20000)
        {
            if (CachedAllTableNames.Count != 0)
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    return [.. CachedAllTableNames];
                }
                else
                {
                    return SearchTablesFromCachedTableNames(keyword);
                }
            }

            var query = @$"
                SELECT 
                    quote_ident(table_schema) || '.' || quote_ident(table_name) AS full_table_name
                FROM 
                    information_schema.tables
                WHERE 
                    table_type = 'BASE TABLE'
                    AND (table_schema NOT IN ('pg_catalog', 'information_schema'))
                    AND table_name ILIKE @keyword
                ORDER BY 
                    table_schema, table_name
                {(maxResult.HasValue ? $"LIMIT {maxResult}" : string.Empty)}";

            await using var command = new NpgsqlCommand(query);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
            var data = await ExecuteQueryAsync(command);
            return data.ToListString();
        }
    }
}
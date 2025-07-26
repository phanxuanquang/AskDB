using AskDB.Commons.Enums;
using Microsoft.AnalysisServices.AdomdClient;
using MySqlConnector;
using System.Data;

namespace DatabaseInteractor.Services
{
    public class PowerBiService : DatabaseInteractionService
    {
        public PowerBiService(string connectionString) : base(connectionString)
        {
            DatabaseType = DatabaseType.PowerBI;
        }

        public new async Task<DataTable> ExecuteQueryAsync(string daxQuery)
        {
            return await Task.Run(() =>
            {
                using var conn = new AdomdConnection(ConnectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = daxQuery;

                using var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                var ds = new DataSet { EnforceConstraints = false };
                ds.Load(reader, LoadOption.OverwriteChanges, "Result");

                return ds.Tables["Result"];
            });
        }

        public new async Task EnsureDatabaseConnectionAsync()
        {
            await Task.Run(() =>
            {
                using var conn = new AdomdConnection(ConnectionString);
                conn.Open();
            });
        }

        public override async Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            if (string.IsNullOrEmpty(table))
            {
                throw new ArgumentException("Table name cannot be null or empty.", nameof(table));
            }

            return await Task.Run(() =>
            {
                using var conn = new AdomdConnection(ConnectionString);
                conn.Open();

                var daxQuery = GetTableStructureDetailQueryTemplate.Replace("{TableName}", table);
                using var cmd = new AdomdCommand(daxQuery, conn);

                using var reader = cmd.ExecuteReader();
                var schemaTable = reader.GetSchemaTable();

                var result = new DataTable("Schema");
                result.Columns.Add("ColumnName", typeof(string));
                result.Columns.Add("DataType", typeof(string));
                result.Columns.Add("IsNullable", typeof(bool));
                result.Columns.Add("IsKey", typeof(bool));
                result.Columns.Add("IsUnique", typeof(bool));

                foreach (DataRow row in schemaTable.Rows)
                {
                    var columnNameRaw = row["ColumnName"]?.ToString();
                    var columnName = columnNameRaw;

                    if (!string.IsNullOrEmpty(columnNameRaw))
                    {
                        int openBracket = columnNameRaw.IndexOf('[');
                        int closeBracket = columnNameRaw.IndexOf(']');
                        if (openBracket >= 0 && closeBracket > openBracket)
                        {
                            columnName = columnNameRaw.Substring(openBracket + 1, closeBracket - openBracket - 1);
                        }
                    }
                    var dataType = row["DataType"]?.ToString();
                    var isNullable = row["AllowDBNull"] is bool allowNull && allowNull;

                    var isKey = row.Table.Columns.Contains("IsKey") && row["IsKey"] is bool key && key;
                    var isUnique = row.Table.Columns.Contains("IsUnique") && row["IsUnique"] is bool unique && unique;

                    result.Rows.Add(columnName, dataType, isNullable, isKey, isUnique);
                }

                return result;
            });
        }

        public override async Task<List<string>> GetUserPermissionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "READ-ONLY",
            });
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

            var dataTable = await ExecuteQueryAsync(SearchTablesByNameQueryTemplate);

            CachedAllTableNames.UnionWith(dataTable
                .AsEnumerable()
                .Select(r => r.Field<string>("NAME")));

            return SearchTablesFromCachedTableNames(keyword);
        }
    }
}
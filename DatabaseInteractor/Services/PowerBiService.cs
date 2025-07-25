using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AskDB.Commons.Enums;

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
            using var conn = new AdomdConnection(ConnectionString);
            await Task.Run(() => conn.Open());

            using var cmd = conn.CreateCommand();
            cmd.CommandText = daxQuery;

            using var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            var ds = new DataSet { EnforceConstraints = false };
            ds.Load(reader, LoadOption.OverwriteChanges, "Result");

            return ds.Tables["Result"];
        }

        public async override Task<DataTable> GetTableStructureDetailAsync(string? schema, string table)
        {
            throw new NotImplementedException();
        }

        public async override Task<List<string>> GetUserPermissionsAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "READ-ONLY",
            });
        }

        public async override Task<List<string>> SearchTablesByNameAsync(string? keyword, int? maxResult = 20000)
        {
            throw new NotImplementedException();
        }
    }
}

using Microsoft.AnalysisServices.AdomdClient;
using System.Data;
using System.Data.Common;

namespace DatabaseInteractor.Extensions
{
    public class PowerBIConnection(string connectionString) : DbConnection
    {
        private readonly AdomdConnection _innerConnection = new(connectionString);

        public override string ConnectionString
        {
            get => _innerConnection.ConnectionString;
            set => _innerConnection.ConnectionString = value;
        }

        public override string Database => _innerConnection.Database;
        public override string DataSource => _innerConnection.Database;
        public override string ServerVersion => "N/A";
        public override ConnectionState State => _innerConnection.State;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Open() => _innerConnection.Open();
        public override void Close() => _innerConnection.Close();

        protected override DbCommand CreateDbCommand()
            => throw new NotSupportedException("Command creation is not supported for Power BI DAX queries.");

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => throw new NotSupportedException("Transaction is not supported for Power BI DAX queries.");
    }
}

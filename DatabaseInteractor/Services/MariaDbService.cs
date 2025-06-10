namespace DatabaseInteractor.Services
{
    public class MariaDbService : MySqlService
    {
        public MariaDbService(string connectionString) : base(connectionString)
        {
            DatabaseType = AskDB.Commons.Enums.DatabaseType.MariaDB;
        }
    }
}

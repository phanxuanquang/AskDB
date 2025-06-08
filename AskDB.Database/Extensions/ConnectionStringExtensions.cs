using AskDB.Commons.Extensions;
using AskDB.Database.Models;

namespace AskDB.Database.Extensions
{
    public static class ConnectionStringExtensions
    {
        public static ConnectionString Encrypt(this ConnectionString connectionString)
        {
            return new ConnectionString
            {
                Value = connectionString.Value.Trim().AesEncrypt(),
                Name = connectionString.Name.Trim().AesEncrypt(),
                DatabaseType = connectionString.DatabaseType,
                LastAccessTime = connectionString.LastAccessTime,
            };
        }

        public static ConnectionString Decrypt(this ConnectionString connectionString)
        {
            return new ConnectionString
            {
                Value = connectionString.Value.Trim().AesDecrypt(),
                Name = connectionString.Name.Trim().AesDecrypt(),
                DatabaseType = connectionString.DatabaseType,
                LastAccessTime = connectionString.LastAccessTime,
            };
        }
    }
}

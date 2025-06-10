using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Database.Models;
using System.Text;

namespace AskDB.Database.Extensions
{
    public static class DatabaseCredentialExtensions
    {
        public static string BuildConnectionString(this DatabaseCredential credential, sbyte timeOutSeconds = 15)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential), "Credential must not be null.");

            if (string.IsNullOrWhiteSpace(credential.Host)) throw new ArgumentNullException("Server must not be empty.", nameof(credential.Host));

            if (string.IsNullOrWhiteSpace(credential.Database)) throw new ArgumentNullException("Database name must not be empty.", nameof(credential.Database));

            if (credential.Port < 0 || credential.Port > 65535) throw new ArgumentOutOfRangeException(nameof(credential.Port), "Port number must be between 0 and 65535.");

            if (credential.DatabaseType != DatabaseType.SqlServer)
            {
                if (string.IsNullOrWhiteSpace(credential.Username)) throw new ArgumentNullException("Username must not be empty.", nameof(credential.Username));

                if (string.IsNullOrWhiteSpace(credential.Password)) throw new ArgumentNullException("Password must not be empty.", nameof(credential.Password));
            }

            var sb = new StringBuilder(256);

            switch (credential.DatabaseType)
            {
                case DatabaseType.SqlServer:
                    BuildSqlServerConnectionString(sb, credential, timeOutSeconds);
                    break;

                case DatabaseType.PostgreSQL:
                    BuildPostgreSqlConnectionString(sb, credential, timeOutSeconds);
                    break;

                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    BuildMySqlConnectionString(sb, credential, timeOutSeconds);
                    break;

                default:
                    throw new NotSupportedException($"Database type '{credential.DatabaseType}' is not supported.");
            }

            return sb.ToString();
        }

        private static void BuildSqlServerConnectionString(StringBuilder sb, DatabaseCredential c, sbyte timeout)
        {
            sb.Append("Server=").Append(c.Host);
            if (c.Port > 0) sb.Append(',').Append(c.Port);
            sb.Append(";Database=").Append(c.Database).Append(';');

            if (string.IsNullOrWhiteSpace(c.Username) || string.IsNullOrWhiteSpace(c.Password))
            {
                sb.Append("Integrated Security=True;");
            }
            else
            {
                sb.Append("User Id=").Append(c.Username).Append(';');
                sb.Append("Password=").Append(c.Password).Append(';');
            }

            sb.Append("TrustServerCertificate=").Append(c.EnableTrustServerCertificate ? "True" : "False").Append(';');
            sb.Append("Encrypt=").Append(c.EnableSsl ? "True" : "False").Append(';');
            sb.Append("Pooling=True;Min Pool Size=5;Max Pool Size=100;");
            sb.Append("Application Name=AskDB;");
            sb.Append("Connection Timeout=").Append(timeout).Append(';');
        }

        private static void BuildPostgreSqlConnectionString(StringBuilder sb, DatabaseCredential c, sbyte timeout)
        {
            sb.Append("Host=").Append(c.Host).Append(';');
            if (c.Port > 0)
                sb.Append("Port=").Append(c.Port).Append(';');

            sb.Append("Database=").Append(c.Database).Append(';');
            sb.Append("Username=").Append(c.Username).Append(';');
            sb.Append("Password=").Append(c.Password).Append(';');
            sb.Append("SSL Mode=").Append(c.EnableSsl ? "Require" : "Disable").Append(';');
            sb.Append("Timeout=").Append(timeout).Append(';');
            sb.Append("Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;");
            sb.Append("No Reset On Close=true;");
            sb.Append("Application Name=AskDB;");
        }

        private static void BuildMySqlConnectionString(StringBuilder sb, DatabaseCredential c, sbyte timeout)
        {
            sb.Append("Server=").Append(c.Host).Append(';');
            if (c.Port > 0)
                sb.Append("Port=").Append(c.Port).Append(';');

            sb.Append("Database=").Append(c.Database).Append(';');
            sb.Append("User=").Append(c.Username).Append(';');
            sb.Append("Password=").Append(c.Password).Append(';');
            sb.Append("SslMode=").Append(c.EnableSsl ? "Required" : "None").Append(';');
            sb.Append("Pooling=true;Min Pool Size=5;Max Pool Size=100;");
            sb.Append("Connection Timeout=").Append(timeout).Append(';');
            sb.Append("Allow User Variables=True;");
            sb.Append("DefaultCommandTimeout=30;");
        }

        public static DatabaseCredential Encrypt(this DatabaseCredential databaseCredential)
        {
            return new DatabaseCredential
            {
                Id = databaseCredential.Id,
                Host = databaseCredential.Host.AesEncrypt(),
                Port = databaseCredential.Port,
                DatabaseType = databaseCredential.DatabaseType,
                Database = databaseCredential.Database.AesEncrypt(),
                Username = databaseCredential.Username.AesEncrypt(),
                Password = databaseCredential.Password.AesEncrypt(),
                EnableSsl = databaseCredential.EnableSsl,
                EnableTrustServerCertificate = databaseCredential.EnableTrustServerCertificate,
                LastAccessTime = databaseCredential.LastAccessTime,
                LastModifiedTime = databaseCredential.LastModifiedTime,
            };
        }

        public static DatabaseCredential Decrypt(this DatabaseCredential databaseCredential)
        {
            return new DatabaseCredential
            {
                Id = databaseCredential.Id,
                Host = databaseCredential.Host.AesDecrypt(),
                Port = databaseCredential.Port,
                DatabaseType = databaseCredential.DatabaseType,
                Database = databaseCredential.Database.AesDecrypt(),
                Username = databaseCredential.Username.AesDecrypt(),
                Password = databaseCredential.Password.AesDecrypt(),
                EnableSsl = databaseCredential.EnableSsl,
                EnableTrustServerCertificate = databaseCredential.EnableTrustServerCertificate,
                LastAccessTime = databaseCredential.LastAccessTime,
                LastModifiedTime = databaseCredential.LastModifiedTime,
            };
        }
    }
}
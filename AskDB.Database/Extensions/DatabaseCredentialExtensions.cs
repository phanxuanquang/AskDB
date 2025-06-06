using AskDB.Commons.Enums;
using AskDB.Commons.Extensions;
using AskDB.Database.Models;
using System.Text;

namespace AskDB.Database.Extensions
{
    public static class DatabaseCredentialExtensions
    {
        public static string BuildConnectionString(this DatabaseCredential databaseCredential, int timeOutInSeconds = 15)
        {
            var sb = new StringBuilder();
            switch (databaseCredential.DatabaseType)
            {
                case DatabaseType.SqlServer:
                    if (databaseCredential.Port != default)
                    {
                        sb.Append($"Server={databaseCredential.Host.Trim()},{databaseCredential.Port};");

                    }
                    else
                    {
                        sb.Append($"Server={databaseCredential.Host.Trim()};");
                    }
                    sb.Append($"Database={databaseCredential.Database.Trim()};");
                    if (string.IsNullOrEmpty(databaseCredential.Username) || string.IsNullOrEmpty(databaseCredential.Password))
                    {
                        sb.Append("Integrated Security=True;");  // Use Windows Authentication if no credentials are provided
                    }
                    else
                    {
                        sb.Append($"User Id={databaseCredential.Username.Trim()};");
                        sb.Append($"Password={databaseCredential.Password.Trim()};");
                    }

                    sb.Append($"TrustServerCertificate={(databaseCredential.EnableTrustServerCertificate ? "True" : "False")};");
                    sb.Append($"Encrypt={(databaseCredential.EnableSsl ? "True" : "False")};");
                    sb.Append($"Connection Timeout={timeOutInSeconds};");
                    break;
                case DatabaseType.PostgreSQL:
                    sb.Append($"Host={databaseCredential.Host.Trim()};");
                    if (databaseCredential.Port != default)
                    {
                        sb.Append($"Port={databaseCredential.Port};");
                    }
                    sb.Append($"Database={databaseCredential.Database.Trim()};");
                    sb.Append($"Username={databaseCredential.Username.Trim()};");
                    sb.Append($"Password={databaseCredential.Password.Trim()};");
                    sb.Append($"SSL Mode={(databaseCredential.EnableSsl ? "Require" : "Disable")};");
                    sb.Append($"Timeout={timeOutInSeconds};");
                    break;
                case DatabaseType.MySQL:
                    sb.Append($"Server={databaseCredential.Host.Trim()};");
                    if (databaseCredential.Port != default)
                    {
                        sb.Append($"Port={databaseCredential.Port};");
                    }
                    sb.Append($"Database={databaseCredential.Database.Trim()};");
                    sb.Append($"User={databaseCredential.Username.Trim()};");
                    sb.Append($"Password={databaseCredential.Password.Trim()};");
                    sb.Append($"SslMode={(databaseCredential.EnableSsl ? "Required" : "None")};");
                    sb.Append($"Connection Timeout={timeOutInSeconds};");
                    break;
                default:
                    throw new NotSupportedException($"Database type '{databaseCredential.DatabaseType}' is not supported for connection string generation.");
            }
            return sb.ToString();
        }

        public static DatabaseCredential Encrypt(this DatabaseCredential databaseCredential)
        {
            return new DatabaseCredential
            {
                Host = databaseCredential.Host.Trim().AesEncrypt(),
                Port = databaseCredential.Port,
                DatabaseType = databaseCredential.DatabaseType,
                Database = databaseCredential.Database.Trim().AesEncrypt(),
                Username = databaseCredential.Username.Trim().AesEncrypt(),
                Password = databaseCredential.Password.Trim().AesEncrypt(),
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
                Host = databaseCredential.Host.Trim().AesDecrypt(),
                Port = databaseCredential.Port,
                DatabaseType = databaseCredential.DatabaseType,
                Database = databaseCredential.Database.Trim().AesDecrypt(),
                Username = databaseCredential.Username.Trim().AesDecrypt(),
                Password = databaseCredential.Password.Trim().AesDecrypt(),
                EnableSsl = databaseCredential.EnableSsl,
                EnableTrustServerCertificate = databaseCredential.EnableTrustServerCertificate,
                LastAccessTime = databaseCredential.LastAccessTime,
                LastModifiedTime = databaseCredential.LastModifiedTime,
            };
        }
    }
}
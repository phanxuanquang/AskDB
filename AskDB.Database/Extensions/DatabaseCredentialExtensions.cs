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
                        sb.Append($"Server={databaseCredential.Host},{databaseCredential.Port};");

                    }
                    else
                    {
                        sb.Append($"Server={databaseCredential.Host};");
                    }
                    sb.Append($"Database={databaseCredential.Database};");
                    if (string.IsNullOrEmpty(databaseCredential.Username) || string.IsNullOrEmpty(databaseCredential.Password))
                    {
                        sb.Append("Integrated Security=True;");  // Use Windows Authentication if no credentials are provided
                    }
                    else
                    {
                        sb.Append($"User Id={databaseCredential.Username};");
                        sb.Append($"Password={databaseCredential.Password};");
                    }

                    sb.Append($"TrustServerCertificate={(databaseCredential.EnableTrustServerCertificate ? "True" : "False")};");
                    sb.Append($"Encrypt={(databaseCredential.EnableSsl ? "True" : "False")};");
                    sb.Append($"Connection Timeout={timeOutInSeconds};");
                    break;
                case DatabaseType.PostgreSQL:
                    sb.Append($"Host={databaseCredential.Host};");
                    if (databaseCredential.Port != default)
                    {
                        sb.Append($"Port={databaseCredential.Port};");
                    }
                    sb.Append($"Database={databaseCredential.Database};");
                    sb.Append($"Username={databaseCredential.Username};");
                    sb.Append($"Password={databaseCredential.Password};");
                    sb.Append($"SSL Mode={(databaseCredential.EnableSsl ? "Require" : "Disable")};");
                    sb.Append($"Timeout={timeOutInSeconds};");
                    break;
                case DatabaseType.MySQL:
                    sb.Append($"Server={databaseCredential.Host};");
                    if (databaseCredential.Port != default)
                    {
                        sb.Append($"Port={databaseCredential.Port};");
                    }
                    sb.Append($"Database={databaseCredential.Database};");
                    sb.Append($"User={databaseCredential.Username};");
                    sb.Append($"Password={databaseCredential.Password};");
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
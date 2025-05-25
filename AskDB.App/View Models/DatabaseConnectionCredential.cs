using DatabaseInteractor.Models.Enums;
using System;

namespace AskDB.App.ViewModels
{
    public class DatabaseConnectionCredential
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
        public bool EnableSsl { get; set; } = false;
        public bool EnableTrustServerCertificate { get; set; } = false;

        public string AsConnectionString(int timeOutInSeconds = 15)
        {
            var sb = new System.Text.StringBuilder();
            switch (DatabaseType)
            {
                case DatabaseType.SqlServer:
                    sb.Append($"Server={Host},{Port};");
                    sb.Append($"Database={Database};");
                    sb.Append($"User Id={Username};");
                    sb.Append($"Password={Password};");
                    sb.Append($"TrustServerCertificate={(EnableTrustServerCertificate ? "True" : "False")};");
                    sb.Append($"Encrypt={(EnableSsl ? "True" : "False")};");
                    sb.Append($"Connection Timeout={timeOutInSeconds};");
                    break;
                case DatabaseType.PostgreSQL:
                    sb.Append($"Host={Host};");
                    sb.Append($"Port={Port};");
                    sb.Append($"Database={Database};");
                    sb.Append($"Username={Username};");
                    sb.Append($"Password={Password};");
                    sb.Append($"SSL Mode={(EnableSsl ? "Require" : "Disable")};");
                    sb.Append($"Timeout={timeOutInSeconds};");
                    break;
                case DatabaseType.MySQL:
                    sb.Append($"Server={Host};");
                    sb.Append($"Port={Port};");
                    sb.Append($"Database={Database};");
                    sb.Append($"User={Username};");
                    sb.Append($"Password={Password};");
                    sb.Append($"SslMode={(EnableSsl ? "Required" : "None")};");
                    sb.Append($"Connection Timeout={timeOutInSeconds};");
                    break;
                default:
                    throw new NotSupportedException($"Database type '{DatabaseType}' is not supported for connection string generation.");
            }
            return sb.ToString();
        }
    }
}

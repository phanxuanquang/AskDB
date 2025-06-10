using AskDB.Commons.Enums;
using System.ComponentModel.DataAnnotations;

namespace AskDB.Database.Models
{
    public class DatabaseCredential
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DatabaseType DatabaseType { get; set; }
        public bool EnableSsl { get; set; }
        public bool EnableTrustServerCertificate { get; set; }
        public DateTime LastModifiedTime { get; set; } = DateTime.Now;
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
    }
}

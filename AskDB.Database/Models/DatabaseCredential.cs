using AskDB.Commons.Enums;
using System.ComponentModel.DataAnnotations;

namespace AskDB.Database.Models
{
    public class DatabaseCredential
    {
        [Key]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public bool EnableSsl { get; set; }
        public bool EnableTrustServerCertificate { get; set; }
        public DateTime LastModifiedTime { get; set; } = DateTime.Now;
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
    }
}

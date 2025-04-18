using Local_Database.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Local_Database.Models
{
    public class DatabaseCredential
    {
        [Key]
        public int Id { get; set; }
        public required string Server { get; set; }
        public int? Port { get; set; }
        public required string Database { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public int? ConnectionTimeout { get; set; } = 15;
        public DateTime LastUpdatedTime { get; set; } = DateTime.Now;
        public bool IsSslEnabled { get; set; }
        public string? ConnectionString { get; set; }
    }
}

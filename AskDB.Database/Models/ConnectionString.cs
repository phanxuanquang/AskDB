using AskDB.Commons.Enums;
using System.ComponentModel.DataAnnotations;

namespace AskDB.Database.Models
{
    public class ConnectionString
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Name { get; set; } = string.Empty;
        public DatabaseType DatabaseType { get; set; }
        public required string Value { get; set; } = string.Empty;
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
    }
}

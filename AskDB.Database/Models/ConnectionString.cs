using AskDB.Commons.Enums;
using System.ComponentModel.DataAnnotations;

namespace AskDB.Database.Models
{
    public class ConnectionString
    {
        [Key]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DatabaseType DatabaseType { get; set; }
        public required string Value { get; set; }
        public DateTime LastModifiedTime { get; set; } = DateTime.Now;
    }
}

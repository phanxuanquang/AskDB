using AskDB.Commons.Enums;
using System.ComponentModel.DataAnnotations;

namespace AskDB.Database.Models
{
    public class ConnectionString
    {
        [Key]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public string Name { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public string Value { get; set; }
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
    }
}

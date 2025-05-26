using System.ComponentModel.DataAnnotations;

namespace AskDB.Database.Models
{
    public class Prompt
    {
        [Key]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public required string CodeName { get; set; }
        public required string Content { get; set; }
        public DateTime LastModifiedTime { get; set; } = DateTime.Now;
    }
}

using System.ComponentModel.DataAnnotations;

namespace AskDB.Database.Models
{
    public class UserSetting
    {
        [Key]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public required string ApiKey { get; set; }
        public DateTime LastModifiedTime { get; set; } = DateTime.Now;
    }
}
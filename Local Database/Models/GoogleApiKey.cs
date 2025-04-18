using System.ComponentModel.DataAnnotations;

namespace Local_Database.Models
{
    public class GoogleApiKey
    {
        [Key]
        public int Id { get; set; }

        public required string Key { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}

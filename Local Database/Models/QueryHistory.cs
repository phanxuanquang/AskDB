using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Local_Database.Models
{
    public class QueryHistory
    {
        [Key]
        public int Id { get; set; }
        public required string SqlQuery { get; set; }

        public string? NaturalLanguageQuery { get; set; }

        public int TotalExecutions { get; set; } = 0;

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        [ForeignKey("DatabaseCredential")]
        public required int DatabaseId { get; set; }

        public virtual DatabaseCredential DatabaseCredential { get; set; }
    }
}

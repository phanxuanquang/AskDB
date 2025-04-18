using Local_Database.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Local_Database.Models
{
    public class SystemInstruction
    {
        [Key]
        public InstructionPurpose Id { get; set; }

        public required string Content { get; set; }
    }
}

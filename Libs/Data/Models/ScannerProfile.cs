using System.ComponentModel.DataAnnotations;

namespace Libs.Data.Models
{
    public class ScannerProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string ProfileName { get; set; } // "HS-1800", "SP-500 Manual", etc.

        [Required]
        public required string StrategyClassName { get; set; } // "HS1800Strategy", "SP500Strategy"

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true; // Soft delete capability

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? DateUpdated { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Libs.Data.Models
{
    public class ProfileConfiguration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProfileId { get; set; }

        public ScannerProfile? Profile { get; set; }

        [Required]
        public required string ConfigKey { get; set; } // "CompletionDelaySeconds", "DirectoryPattern", etc.

        [Required]
        public required string ConfigValue { get; set; } // "25", "{WatchedDir}/{YYYYMMDD}/*", etc.

        public string? Description { get; set; }
    }
}

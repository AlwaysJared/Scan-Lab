using System.ComponentModel.DataAnnotations;

namespace Libs.Data.Models
{
    public class Scanner
    {
        [Key]
        public required Guid Id { get; set; }
        public required string ScannerName { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? WatchedDir { get; set; }
        public string? DestinationDir { get; set; }
        public string? ArchiveDir { get; set; }
        public string? ArtistName { get; set; }
    }
}
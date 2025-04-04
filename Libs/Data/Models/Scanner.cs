using System.ComponentModel.DataAnnotations;

namespace Libs.Data.Models
{
    public class Scanner
    {
        public Scanner(){}
        public Scanner(Scanner? scnr){
            Id = scnr?.Id ?? new Guid();
            ScannerName = scnr?.ScannerName ?? "";
            Make = scnr?.Make ?? "";
            Model = scnr?.Model ?? "";
            ArtistName = scnr?.ArtistName ?? "";
            WatchedDir = scnr?.WatchedDir ?? "";
            DestinationDir = scnr?.DestinationDir ?? "";
            ArchiveDir = scnr?.ArchiveDir ?? "";
        }

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string ScannerName { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public required string WatchedDir { get; set; }
        public required string DestinationDir { get; set; }
        public required string ArchiveDir { get; set; }
        public string? ArtistName { get; set; }
    }
}
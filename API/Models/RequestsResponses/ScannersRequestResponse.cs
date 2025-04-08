using System;
using Libs.Data.Models;

namespace API.Models.RequestsResponses
{
    public class AddScannerRequest
    {
        public required string ScannerName { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public required string WatchedDir { get; set; }
        public required string DestinationDir { get; set; }
        public required string ArchiveDir { get; set; }
        public string? ArtistName { get; set; }
    }

    public class AddScannerResponse : BaseResponse
    {
        public string? ScannerId { get; set; }
    }


    public class DeleteScannerRequest
    {
        public required string OrderId { get; set; }
    }

    public class DeleteScannerResponse : BaseResponse
    {
        public string? ScannerId { get; set; }
    }

    public class UpdateScannerRequest : BaseRequest
    {
        public required Scanner Scnr{ get; set; }
    }
}
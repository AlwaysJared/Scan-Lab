using System;

namespace API.Models
{
    public class AddScannerRequest
    {
        public required string OrderId { get; set; }
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
}
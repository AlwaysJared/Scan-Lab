using System;
using Libs.Data.Models;

namespace API.Models
{
    public class SubmitOrderRequest
    {
        public required string OrderId { get; set; }
        public required Guid ScannerId { get; set; }
        public List<Roll>? Rolls { get; set; }
    }

    public class SubmitOrderResponse
    {
        public string? OrderId { get; set; }
        public string? Message { get; set; }
    }

}
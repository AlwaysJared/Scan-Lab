using System;

namespace API.Models
{
    public class SubmitOrderRequest
    {
        
        public required string OrderId { get; set; }
    }

    public class SubmitOrderResponse
    {
        public string? OrderId { get; set; }
        public string? Message { get; set; }
    }

}
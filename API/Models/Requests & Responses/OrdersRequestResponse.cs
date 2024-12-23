using System;
using Libs.Data.Models;

namespace API.Models
{
    public class SubmitOrderRequest : BaseRequest
    {
        public required string OrderId { get; set; }
        public required Scanner Scanner { get; set; }
        public Customer? Customer { get; set; }
        public List<Roll>? Rolls { get; set; }
    }

    public class SubmitOrderResponse : BaseResponse
    {
        public string? OrderId { get; set; }
    }

    public class CompleteOrderRequest : BaseRequest
    {
        public required string OrderId { get; set; }
    }

    public class CompleteOrderResponse : BaseResponse
    {

    }
}
using System;
using Libs.Data.Models;
using Libs.Enums;

namespace API.Models.RequestsResponses
{
    public class SubmitOrderRequest : BaseRequest
    {
        public required string OrderId { get; set; }
        public required Scanner Scanner { get; set; }
        public Customer? Customer { get; set; }
        public string? CustomerInitials { get; set; }
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

    public class GetOrdersRequest : BaseRequest
    {
        public string? search { get; set; }
        public OrderStatus? orderStatus { get; set; }
        public Guid? scannerId { get; set; }
        public bool fetchCompletedOrders { get; set; } = false;
    }

    public class DeleteOrderRequest : BaseRequest
    {
        public required string OrderId { get; set; } = "";
    }
}
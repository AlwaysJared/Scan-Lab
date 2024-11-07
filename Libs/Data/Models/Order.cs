namespace Libs.Data.Models
{
    public class Order
    {
        public required string OrderId { get; set; }
        public string? CustomerId { get; set; }
        public OrderStatus Status { get; set; }
    }
}
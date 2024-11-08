using System.ComponentModel.DataAnnotations;

namespace Libs.Data.Models
{
    public class Order
    {
        [Key]
        public required string OrderId { get; set; }
        public string? CustomerId { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Created;
        public List<Roll>? Rolls{ get; set; }
        public Scanner? Scanner{ get; set; }
        public DateTime? DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateUpdated { get; set; }
    }
}
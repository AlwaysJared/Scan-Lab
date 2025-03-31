using System.ComponentModel.DataAnnotations;
using Libs.Enums;

namespace Libs.Data.Models
{
    public class Order
    {
        [Key]
        public string OrderId { get; set; }
        public Customer? Customer { get; set; }
        public string? CustomerInitials { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Created;
        public List<Roll>? Rolls{ get; set; }
        public Scanner? Scanner{ get; set; }
        public DateTime? DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateUpdated { get; set; }
    }
}
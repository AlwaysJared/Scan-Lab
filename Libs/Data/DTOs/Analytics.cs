using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Data.DTOs.Analytics
{
    public class AnalyticsBaseDTO
    {
        public required string Id { get; set;}
        public required string Name { get; set; }
        public double Value { get; set; }
    }

    public class OrdersPerStaffDTO
    {
        public List<AnalyticsBaseDTO> Analytics { get; set; } = new();
    }
}
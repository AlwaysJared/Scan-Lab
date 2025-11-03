using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Data.DTOs.Analytics
{
    public class AnalyticsBaseDTO
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }

    public class OrdersPerStaffDTO
    {
        List<AnalyticsBaseDTO> Analytics { get; set; }
    }
}
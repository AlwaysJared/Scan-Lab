using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Enums;

namespace API.Models.RequestsResponses.Analytics
{
    public class AnalyticsRequest : BaseRequest
    {
        public List<Guid>? Ids { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsAverage { get; set; } = true;
        public IntervalType? Interval { get; set; }
    }
    public class AnalyticsResponse : BaseResponse
    {
        public double Value { get; set; }
    }
}
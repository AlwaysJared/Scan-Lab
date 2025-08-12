using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Data.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Models.RequestsResponses
{
    public class GetLogsRequest : BaseRequest
    {
        public string? level { get; set; }
        public string? area { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 10;
    }

    public class GetLogsResponse : BaseResponse
    {
        public int totalPages { get; set; } = 0;
        public List<LogEntry> logs { get; set; } = new List<LogEntry>();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Data.Models;

namespace Admin.Models.DTOs
{
    public class Logs
    {
        public class GetLogsDTO
        {
            public List<LogEntry> Logs { get; set; } = new List<LogEntry>();
            public int TotalPages { get; set; } = 0;
        }
    }
}
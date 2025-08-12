using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Data.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public string MessageTemplate { get; set; } = "";
        public string Level { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string? Exception { get; set; }
        public string LogEvent { get; set; } = "";
        public string Properties { get; set; } = "";
        public string Area { get; set; } = "System";
    }
}
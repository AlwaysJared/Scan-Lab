using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Models;

namespace Libs.Interfaces
{
    public interface ILogRepository : IDisposable
    {
        Task<SystemResponse> GetLogs(string? level, string? area, int page, int pageSize);
    }
}
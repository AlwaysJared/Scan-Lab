using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Libs.Repositories
{
    public class LogRepository : ILogRepository, IDisposable
    {
        private ScanLab_LogContext logContext;
        public LogRepository(ScanLab_LogContext logContext)
        {
            this.logContext = logContext;
        }

        public void Dispose()
        {
            logContext.Dispose();
        }

        public async Task<SystemResponse> GetLogs(string? level, string? area, int page=1, int pageSize=10)
        {
            try
            {
                var totalPages = 0;
                var logs = await logContext.Logs
                    .ToListAsync();

                if (!String.IsNullOrEmpty(level))
                { 
                    logs = logs.Where(l => l.Level.ToLower() == level.ToLower()).ToList();
                }
                
                if (!String.IsNullOrEmpty(area))
                { 
                    logs = logs.Where(l => l.Area.ToLower() == area.ToLower()).ToList();
                }

                totalPages = (int)Math.Ceiling((logs?.Count ?? 0) / (double)pageSize);

                logs = logs.Skip((page-1) * pageSize).Take(pageSize).ToList();

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = new { logs = logs, totalPages=totalPages }
                };
            }
            catch (Exception ex)
            {
                return new SystemResponse
                {
                    IsSuccess = false,
                    ReturnObject = ex
                };
            }
        }
    }
}
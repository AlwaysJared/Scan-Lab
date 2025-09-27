using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;

namespace Libs.Interfaces
{
    public interface IAnalyticsRepository : IDisposable
    {
        Task<SystemResponse> RollsPerStaffInTimeframe(List<Guid> staffIds, DateTime startDate, DateTime endDate, bool isAverage);
        Task<SystemResponse> OrdersPerStaffInTimeframe(List<Guid> staffIds, DateTime startDate, DateTime endDate, bool isAverage);
        Task<SystemResponse> RollsPerScannerInTimeframe(List<Guid> scannerIds, DateTime startDate, DateTime endDate, bool isAverage);
        Task<SystemResponse> OrdersPerScannerInTimeframe(List<Guid> scannerIds, DateTime startDate, DateTime endDate, bool isAverage);
    }
}
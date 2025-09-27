using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Libs.Repositories
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly ScanLabContext _context;

        public AnalyticsRepository(ScanLabContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<SystemResponse> OrdersPerScannerInTimeframe(List<Guid> scannerIds, DateTime startDate, DateTime endDate, bool isAverage)
        {
            throw new NotImplementedException();
        }

        public async Task<SystemResponse> OrdersPerStaffInTimeframe(List<Guid> staffIds, DateTime startDate, DateTime endDate, bool isAverage)
        {
            throw new NotImplementedException();
        }

        public async Task<SystemResponse> RollsPerScannerInTimeframe(List<Guid> scannerIds, DateTime startDate, DateTime endDate, bool isAverage)
        {
            throw new NotImplementedException();
        }

        public async Task<SystemResponse> RollsPerStaffInTimeframe(List<Guid> staffIds, DateTime startDate, DateTime endDate, bool isAverage)
        {
            try
            {
                var query = _context.Rolls
                    .AsNoTracking()
                    .Include(r => r.Order)
                    .Where(r => r.Status == Enums.RollStatus.Processed)
                    .Where(r => staffIds.Any() ? staffIds.Contains(r.Order.CreatedBy.Value) : true)
                    .Where(r => r.DateUpdated.Value >= startDate && r.DateUpdated.Value <= endDate);

                if (isAverage)
                {
                    // Group by date, count per day, then average
                    var average = await query
                        .GroupBy(o => o.DateUpdated.Value)
                        .Select(g => g.Count())
                        .AverageAsync();

                    return new SystemResponse{
                        IsSuccess = true,
                        ReturnObject = average
                    };
                }
                else
                {
                    // Return total count
                    var total = await query.CountAsync();
                    return new SystemResponse{
                        IsSuccess = true,
                        ReturnObject = total
                    };
                }
            }
            catch (Exception ex)
            {

                return new SystemResponse
                {
                    IsSuccess = false,
                    Message = ex.Message,
                    ReturnObject = ex
                };
            }
        }
    }
}
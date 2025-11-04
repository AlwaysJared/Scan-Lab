using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.DTOs.Analytics;
using Libs.Data.Models;
using Libs.Enums;
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

        public async Task<SystemResponse> OrdersPerScannerInTimeframe(
                   List<Guid>? scannerIds,
                   DateTime? startDate,
                   DateTime? endDate,
                   bool isAverage,
                   IntervalType? interval)
        {
            try
            {
                // 🕐 Get first-ever order (for default start date)
                DateTime? firstOrderDate = await _context.Orders
                    .OrderBy(o => o.DateCreated)
                    .Select(o => o.DateCreated)
                    .FirstOrDefaultAsync();

                // 🗓 Determine effective timeframe
                DateTime effectiveStart = startDate ?? firstOrderDate ?? DateTime.Today;
                DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

                DateTime utcStart = effectiveStart.ToUniversalTime();
                DateTime utcEnd = effectiveEnd.ToUniversalTime();

                interval ??= IntervalType.Day;

                // 🧮 Build query base
                var query = _context.Orders
                    .AsNoTracking()
                    .Include(r => r.Scanner)
                    .Where(r => r.Status == Enums.OrderStatus.Completed)
                    .Where(r => r.DateUpdated >= utcStart && r.DateUpdated <= utcEnd);

                // 💡 If no scannerIds provided, use all staff with completed orders
                if (scannerIds == null || !scannerIds.Any())
                {
                    scannerIds = await _context.Scanners.Select(s => s.Id).ToListAsync();
                }

                // Filter to relevant scanner
                query = query.Where(o => scannerIds.Contains(o.Scanner.Id));

                // 📊 Count orders per staff
                var groupedByScanner = await query
                    .GroupBy(r => r.Scanner.Id)
                    .Select(g => new
                    {
                        ScannerId = g.Key,
                        TotalCount = g.Count()
                    })
                    .ToListAsync();

                // 👥 Get scanner names
                var scannerInfo = await _context.Scanners
                    .Where(s => scannerIds.Contains(s.Id))
                    .Select(s => new
                    {
                        s.Id,
                        ScannerName = (s.ScannerName).Trim()
                    })
                    .ToListAsync();

                var results = new List<AnalyticsBaseDTO>();

                // 🧠 Compute totals or averages
                foreach (var id in scannerIds)
                {
                    var staffData = groupedByScanner.FirstOrDefault(g => g.ScannerId == id);
                    double totalCount = staffData?.TotalCount ?? 0;

                    double value;
                    if (isAverage)
                    {
                        double intervalCount = interval switch
                        {
                            IntervalType.Day => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays, 1),
                            IntervalType.Week => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays / 7.0, 1),
                            IntervalType.Month => Math.Max(((effectiveEnd.Year - effectiveStart.Year) * 12) +
                                                           (effectiveEnd.Month - effectiveStart.Month) + 1, 1),
                            IntervalType.Hour => Math.Max((effectiveEnd - effectiveStart).TotalHours, 1),
                            _ => 1
                        };

                        value = Math.Round(totalCount / intervalCount, 2);
                    }
                    else
                    {
                        value = totalCount;
                    }

                    var scanner = scannerInfo.FirstOrDefault(s => s.Id == id);
                    string name = scanner?.ScannerName ?? "Unknown Scanner";

                    results.Add(new AnalyticsBaseDTO
                    {
                        Id = id.ToString(),
                        Name = name,
                        Value = value
                    });
                }

                var dto = new OrdersPerStaffDTO
                {
                    Analytics = results.OrderBy(r => r.Name).ToList()
                };

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = dto
                };
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


        // public async Task<SystemResponse> OrdersPerScannerInTimeframe(
        //     List<Guid>? scannerIds,
        //     DateTime? startDate,
        //     DateTime? endDate,
        //     bool isAverage,
        //     IntervalType? interval
        // )
        // {
        //     try
        //     {
        //         DateTime? utcStart = startDate?.ToUniversalTime();
        //         DateTime? utcEnd = endDate?.ToUniversalTime();

        //         var orderDatesSorted = _context.Orders.OrderBy(o => o.CreatedBy)
        //             .Select(o => o.DateCreated);

        //         DateTime? firstOrderDate = await orderDatesSorted.FirstOrDefaultAsync();
        //         // DateTime? lastOrderDate = await orderDatesSorted.LastOrDefaultAsync();

        //         // if((firstOrderDate ?? DateTime.MinValue) == (lastOrderDate ?? DateTime.MinValue))
        //         // {
        //         //     lastOrderDate = null;
        //         // }


        //         DateTime effectiveStart = startDate ?? firstOrderDate ?? DateTime.Today;
        //         DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

        //         DateTime localeffectiveStart = startDate ?? firstOrderDate ?? DateTime.Today;
        //         DateTime localeffectiveEnd = endDate ?? DateTime.Today.AddDays(1);


        //         var query = _context.Orders
        //             .Include(o => o.Scanner)
        //             .AsNoTracking()
        //             .Where(o => o.Status == Enums.OrderStatus.Completed)
        //             .Where(o => (scannerIds != null && scannerIds.Any()) ? scannerIds.Contains(o.Scanner.Id) : true)
        //             .Where(o => (!utcStart.HasValue || o.DateUpdated.Value >= utcStart)
        //                      && (!utcEnd.HasValue || o.DateUpdated.Value <= utcEnd));

        //         if (!isAverage)
        //         {
        //             var total = await query.CountAsync();
        //             return new SystemResponse
        //             {
        //                 IsSuccess = true,
        //                 ReturnObject = total
        //             };
        //         }

        //         interval ??= IntervalType.Day;
        //         var duration = effectiveEnd - effectiveStart;

        //         ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

        //         List<GroupedRollResult> groupedWithZeros;

        //         if (interval == IntervalType.Week)
        //         {
        //             var orderDates = await query
        //                 .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
        //                 .ToListAsync();

        //             var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
        //                 .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .Select(g => new
        //                 {
        //                     g.Key.Year,
        //                     g.Key.Week,
        //                     Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
        //                 })
        //                 .ToList();

        //             var orderGroups = orderDates
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .ToDictionary(
        //                     g => $"Week {g.Key.Week:00} ({g.Key.Year})",
        //                     g => g.Count()
        //                 );

        //             groupedWithZeros = allWeekDates
        //                 .Select(w => new GroupedRollResult
        //                 {
        //                     Interval = w.Label,
        //                     Count = orderGroups.TryGetValue(w.Label, out int count) ? count : 0
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Day)
        //         {
        //             int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
        //             var allDates = Enumerable.Range(0, totalDays)
        //                 .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allDates
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Hour)
        //         {
        //             int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
        //             var allHours = Enumerable.Range(0, totalHours)
        //                 .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day,
        //                     r.DateUpdated.Value.Hour
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allHours
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd HH:00");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Month)
        //         {
        //             DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
        //             DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
        //             int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

        //             var allMonths = Enumerable.Range(0, totalMonths)
        //                 .Select(i => firstMonth.AddMonths(i))
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allMonths
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else
        //         {
        //             return new SystemResponse
        //             {
        //                 IsSuccess = false,
        //                 Message = "Unsupported interval type."
        //             };
        //         }

        //         double average = groupedWithZeros.Any()
        //             ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
        //             : 0;

        //         return new SystemResponse
        //         {
        //             IsSuccess = true,
        //             ReturnObject = average
        //         };
        //     }
        //     catch (Exception ex)
        //     {
        //         return new SystemResponse
        //         {
        //             IsSuccess = false,
        //             Message = ex.Message,
        //             ReturnObject = ex
        //         };
        //     }
        // }

        public async Task<SystemResponse> OrdersPerStaffInTimeframe(
           List<Guid>? staffIds,
           DateTime? startDate,
           DateTime? endDate,
           bool isAverage,
           IntervalType? interval)
        {
            try
            {
                // 🕐 Get first-ever order (for default start date)
                DateTime? firstOrderDate = await _context.Orders
                    .OrderBy(o => o.DateCreated)
                    .Select(o => o.DateCreated)
                    .FirstOrDefaultAsync();

                // 🗓 Determine effective timeframe
                DateTime effectiveStart = startDate ?? firstOrderDate ?? DateTime.Today;
                DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

                DateTime utcStart = effectiveStart.ToUniversalTime();
                DateTime utcEnd = effectiveEnd.ToUniversalTime();

                interval ??= IntervalType.Day;

                // 🧮 Build query base
                var query = _context.Orders
                    .AsNoTracking()
                    .Where(r => r.Status == Enums.OrderStatus.Completed)
                    .Where(r => r.DateUpdated >= utcStart && r.DateUpdated <= utcEnd);

                // 💡 If no staffIds provided, use all staff with completed orders
                if (staffIds == null || !staffIds.Any())
                {
                    // staffIds = await _context.Orders
                    //     .Where(o => o.UpdatedBy.HasValue)
                    //     .Select(o => o.UpdatedBy.Value)
                    //     .Distinct()
                    //     .ToListAsync();

                    staffIds = await _context.Staff.Select(s => s.Id).ToListAsync();
                }

                // Filter to relevant staff
                query = query.Where(r => r.UpdatedBy.HasValue && staffIds.Contains(r.UpdatedBy.Value));

                // 📊 Count orders per staff
                var groupedByStaff = await query
                    .GroupBy(r => r.UpdatedBy.Value)
                    .Select(g => new
                    {
                        StaffId = g.Key,
                        TotalCount = g.Count()
                    })
                    .ToListAsync();

                // 👥 Get staff names
                var staffInfo = await _context.Staff
                    .Where(s => staffIds.Contains(s.Id))
                    .Select(s => new
                    {
                        s.Id,
                        FullName = (s.FirstName + " " + s.LastName).Trim()
                    })
                    .ToListAsync();

                var results = new List<AnalyticsBaseDTO>();

                // 🧠 Compute totals or averages
                foreach (var staffId in staffIds)
                {
                    var staffData = groupedByStaff.FirstOrDefault(g => g.StaffId == staffId);
                    double totalCount = staffData?.TotalCount ?? 0;

                    double value;
                    if (isAverage)
                    {
                        double intervalCount = interval switch
                        {
                            IntervalType.Day => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays, 1),
                            IntervalType.Week => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays / 7.0, 1),
                            IntervalType.Month => Math.Max(((effectiveEnd.Year - effectiveStart.Year) * 12) +
                                                           (effectiveEnd.Month - effectiveStart.Month) + 1, 1),
                            IntervalType.Hour => Math.Max((effectiveEnd - effectiveStart).TotalHours, 1),
                            _ => 1
                        };

                        value = Math.Round(totalCount / intervalCount, 2);
                    }
                    else
                    {
                        value = totalCount;
                    }

                    var staff = staffInfo.FirstOrDefault(s => s.Id == staffId);
                    string name = staff?.FullName ?? "Unknown Staff";

                    results.Add(new AnalyticsBaseDTO
                    {
                        Id = staffId.ToString(),
                        Name = name,
                        Value = value
                    });
                }

                var dto = new OrdersPerStaffDTO
                {
                    Analytics = results.OrderBy(r => r.Name).ToList()
                };

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = dto
                };
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

        // public async Task<SystemResponse> OrdersPerStaffInTimeframe(
        //     List<Guid>? staffIds,
        //     DateTime? startDate,
        //     DateTime? endDate,
        //     bool isAverage,
        //     IntervalType? interval
        // )
        // {
        //     try
        //     {
        //         DateTime? utcStart = startDate?.ToUniversalTime();
        //         DateTime? utcEnd = endDate?.ToUniversalTime();

        //         var orderDatesSorted = _context.Orders.OrderBy(o => o.CreatedBy)
        //             .Select(o => o.DateCreated);

        //         DateTime? firstOrderDate = await orderDatesSorted.FirstOrDefaultAsync();
        //         // DateTime? lastOrderDate = await orderDatesSorted.LastOrDefaultAsync();

        //         DateTime effectiveStart = startDate ?? firstOrderDate ?? DateTime.Today;
        //         DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

        //         DateTime localeffectiveStart = startDate ?? firstOrderDate ?? DateTime.Today;
        //         DateTime localeffectiveEnd = endDate ?? DateTime.Today.AddDays(1);

        //         var query = _context.Orders
        //             .AsNoTracking()
        //             .Where(r => r.Status == Enums.OrderStatus.Completed)
        //             .Where(r => (staffIds != null && staffIds.Any()) ? staffIds.Contains(r.UpdatedBy.Value) : true)
        //             .Where(r => (!utcStart.HasValue || r.DateUpdated.Value >= utcStart)
        //                      && (!utcEnd.HasValue || r.DateUpdated.Value <= utcEnd));

        //         if (!isAverage)
        //         {
        //             var total = await query.CountAsync();
        //             return new SystemResponse
        //             {
        //                 IsSuccess = true,
        //                 ReturnObject = total
        //             };
        //         }

        //         interval ??= IntervalType.Day;
        //         var duration = effectiveEnd - effectiveStart;

        //         ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

        //         List<GroupedRollResult> groupedWithZeros;

        //         if (interval == IntervalType.Week)
        //         {
        //             var orderDates = await query
        //                 .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
        //                 .ToListAsync();

        //             var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
        //                 .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .Select(g => new
        //                 {
        //                     g.Key.Year,
        //                     g.Key.Week,
        //                     Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
        //                 })
        //                 .ToList();

        //             var orderGroups = orderDates
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .ToDictionary(
        //                     g => $"Week {g.Key.Week:00} ({g.Key.Year})",
        //                     g => g.Count()
        //                 );

        //             groupedWithZeros = allWeekDates
        //                 .Select(w => new GroupedRollResult
        //                 {
        //                     Interval = w.Label,
        //                     Count = orderGroups.TryGetValue(w.Label, out int count) ? count : 0
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Day)
        //         {
        //             int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
        //             var allDates = Enumerable.Range(0, totalDays)
        //                 .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allDates
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Hour)
        //         {
        //             int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
        //             var allHours = Enumerable.Range(0, totalHours)
        //                 .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day,
        //                     r.DateUpdated.Value.Hour
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allHours
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd HH:00");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Month)
        //         {
        //             DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
        //             DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
        //             int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

        //             var allMonths = Enumerable.Range(0, totalMonths)
        //                 .Select(i => firstMonth.AddMonths(i))
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allMonths
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else
        //         {
        //             return new SystemResponse
        //             {
        //                 IsSuccess = false,
        //                 Message = "Unsupported interval type."
        //             };
        //         }

        //         double average = groupedWithZeros.Any()
        //             ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
        //             : 0;

        //         return new SystemResponse
        //         {
        //             IsSuccess = true,
        //             ReturnObject = average
        //         };
        //     }
        //     catch (Exception ex)
        //     {
        //         return new SystemResponse
        //         {
        //             IsSuccess = false,
        //             Message = ex.Message,
        //             ReturnObject = ex
        //         };
        //     }
        // }


        public async Task<SystemResponse> RollsPerScannerInTimeframe(
               List<Guid>? scannerIds,
               DateTime? startDate,
               DateTime? endDate,
               bool isAverage,
               IntervalType? interval)
        {
            try
            {
                // 🕐 Get first-ever order (for default start date)
                DateTime? firstRollDate = await _context.Rolls
                    .OrderBy(o => o.DateCreated)
                    .Select(o => o.DateCreated)
                    .FirstOrDefaultAsync();

                // 🗓 Determine effective timeframe
                DateTime effectiveStart = startDate ?? firstRollDate ?? DateTime.Today;
                DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

                DateTime utcStart = effectiveStart.ToUniversalTime();
                DateTime utcEnd = effectiveEnd.ToUniversalTime();

                interval ??= IntervalType.Day;

                // 🧮 Build query base
                var query = _context.Rolls
                    .AsNoTracking()
                    .Include(r => r.Order)
                    .Include(r => r.Order.Scanner)
                    .Where(r => r.Status == Enums.RollStatus.Processed)
                    .Where(r => r.DateUpdated >= utcStart && r.DateUpdated <= utcEnd);

                // 💡 If no scannerIds provided, use all staff with completed orders
                if (scannerIds == null || !scannerIds.Any())
                {
                    scannerIds = await _context.Scanners.Select(s => s.Id).ToListAsync();
                }

                // Filter to relevant scanner
                query = query.Where(r => scannerIds.Contains(r.Order.Scanner.Id));

                // 📊 Count orders per staff
                var groupedByScanner = await query
                    .GroupBy(r => r.Order.Scanner.Id)
                    .Select(g => new
                    {
                        ScannerId = g.Key,
                        TotalCount = g.Count()
                    })
                    .ToListAsync();

                // 👥 Get scanner names
                var scannerInfo = await _context.Scanners
                    .Where(s => scannerIds.Contains(s.Id))
                    .Select(s => new
                    {
                        s.Id,
                        ScannerName = (s.ScannerName).Trim()
                    })
                    .ToListAsync();

                var results = new List<AnalyticsBaseDTO>();

                // 🧠 Compute totals or averages
                foreach (var id in scannerIds)
                {
                    var staffData = groupedByScanner.FirstOrDefault(g => g.ScannerId == id);
                    double totalCount = staffData?.TotalCount ?? 0;

                    double value;
                    if (isAverage)
                    {
                        double intervalCount = interval switch
                        {
                            IntervalType.Day => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays, 1),
                            IntervalType.Week => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays / 7.0, 1),
                            IntervalType.Month => Math.Max(((effectiveEnd.Year - effectiveStart.Year) * 12) +
                                                           (effectiveEnd.Month - effectiveStart.Month) + 1, 1),
                            IntervalType.Hour => Math.Max((effectiveEnd - effectiveStart).TotalHours, 1),
                            _ => 1
                        };

                        value = Math.Round(totalCount / intervalCount, 2);
                    }
                    else
                    {
                        value = totalCount;
                    }

                    var scanner = scannerInfo.FirstOrDefault(s => s.Id == id);
                    string name = scanner?.ScannerName ?? "Unknown Scanner";

                    results.Add(new AnalyticsBaseDTO
                    {
                        Id = id.ToString(),
                        Name = name,
                        Value = value
                    });
                }

                var dto = new OrdersPerStaffDTO
                {
                    Analytics = results.OrderBy(r => r.Name).ToList()
                };

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = dto
                };
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



        // public async Task<SystemResponse> RollsPerScannerInTimeframe(
        //     List<Guid>? scannerIds,
        //     DateTime? startDate,
        //     DateTime? endDate,
        //     bool isAverage,
        //     IntervalType? interval)
        // {
        //     try
        //     {
        //         DateTime? utcStart = startDate?.ToUniversalTime();
        //         DateTime? utcEnd = endDate?.ToUniversalTime();

        //         var rollDatesSorted = _context.Rolls.OrderBy(o => o.CreatedBy)
        //             .Select(o => o.DateCreated);

        //         DateTime? firstRollDate = await rollDatesSorted.FirstOrDefaultAsync();
        //         // DateTime? lastRollDate = await rollDatesSorted.LastOrDefaultAsync();

        //         DateTime effectiveStart = startDate ?? firstRollDate ?? DateTime.Today;
        //         DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

        //         DateTime localeffectiveStart = startDate ?? firstRollDate ?? (DateTime.Today);
        //         DateTime localeffectiveEnd = endDate ?? (DateTime.Today.AddDays(1));

        //         var query = _context.Rolls
        //             .Include(r => r.Order)
        //             .Include(r => r.Order.Scanner)
        //             .AsNoTracking()
        //             .Where(r => r.Status == Enums.RollStatus.Processed)
        //             .Where(r => (scannerIds != null && scannerIds.Any()) ? scannerIds.Contains(r.Order.Scanner.Id) : true)
        //             .Where(r => (!utcStart.HasValue || r.DateUpdated.Value >= utcStart)
        //                      && (!utcEnd.HasValue || r.DateUpdated.Value <= utcEnd));

        //         if (!isAverage)
        //         {
        //             var total = await query.CountAsync();
        //             return new SystemResponse
        //             {
        //                 IsSuccess = true,
        //                 ReturnObject = total
        //             };
        //         }

        //         interval ??= IntervalType.Day;
        //         var duration = effectiveEnd - effectiveStart;

        //         ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

        //         List<GroupedRollResult> groupedWithZeros;

        //         if (interval == IntervalType.Week)
        //         {
        //             var rollDates = await query
        //                 .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
        //                 .ToListAsync();

        //             var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
        //                 .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .Select(g => new
        //                 {
        //                     g.Key.Year,
        //                     g.Key.Week,
        //                     Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
        //                 })
        //                 .ToList();

        //             var rollGroups = rollDates
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .ToDictionary(
        //                     g => $"Week {g.Key.Week:00} ({g.Key.Year})",
        //                     g => g.Count()
        //                 );

        //             groupedWithZeros = allWeekDates
        //                 .Select(w => new GroupedRollResult
        //                 {
        //                     Interval = w.Label,
        //                     Count = rollGroups.TryGetValue(w.Label, out int count) ? count : 0
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Day)
        //         {
        //             int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
        //             var allDates = Enumerable.Range(0, totalDays)
        //                 .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allDates
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Hour)
        //         {
        //             int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
        //             var allHours = Enumerable.Range(0, totalHours)
        //                 .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day,
        //                     r.DateUpdated.Value.Hour
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allHours
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd HH:00");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Month)
        //         {
        //             DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
        //             DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
        //             int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

        //             var allMonths = Enumerable.Range(0, totalMonths)
        //                 .Select(i => firstMonth.AddMonths(i))
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allMonths
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else
        //         {
        //             return new SystemResponse
        //             {
        //                 IsSuccess = false,
        //                 Message = "Unsupported interval type."
        //             };
        //         }

        //         double average = groupedWithZeros.Any()
        //             ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
        //             : 0;

        //         return new SystemResponse
        //         {
        //             IsSuccess = true,
        //             ReturnObject = average
        //         };
        //     }
        //     catch (Exception ex)
        //     {
        //         return new SystemResponse
        //         {
        //             IsSuccess = false,
        //             Message = ex.Message,
        //             ReturnObject = ex
        //         };
        //     }
        // }

        public async Task<SystemResponse> RollsPerStaffInTimeframe(
               List<Guid>? staffIds,
               DateTime? startDate,
               DateTime? endDate,
               bool isAverage,
               IntervalType? interval)
        {
            try
            {
                // 🕐 Get first-ever order (for default start date)
                DateTime? firstRollDate = await _context.Rolls
                    .OrderBy(o => o.DateCreated)
                    .Select(o => o.DateCreated)
                    .FirstOrDefaultAsync();

                // 🗓 Determine effective timeframe
                DateTime effectiveStart = startDate ?? firstRollDate ?? DateTime.Today;
                DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

                DateTime utcStart = effectiveStart.ToUniversalTime();
                DateTime utcEnd = effectiveEnd.ToUniversalTime();

                interval ??= IntervalType.Day;

                // 🧮 Build query base
                var query = _context.Rolls
                    .AsNoTracking()
                    .Where(r => r.Status == Enums.RollStatus.Processed)
                    .Where(r => r.DateUpdated >= utcStart && r.DateUpdated <= utcEnd);

                // 💡 If no staffIds provided, use all staff with completed orders
                if (staffIds == null || !staffIds.Any())
                {
                    // staffIds = await _context.Rolls
                    //     .Where(o => o.UpdatedBy.HasValue)
                    //     .Select(o => o.UpdatedBy.Value)
                    //     .Distinct()
                    //     .ToListAsync();

                    staffIds = await _context.Staff.Select(s => s.Id).ToListAsync();
                }

                // Filter to relevant staff
                query = query.Where(r => r.UpdatedBy.HasValue && staffIds.Contains(r.UpdatedBy.Value));

                // 📊 Count orders per staff
                var groupedByStaff = await query
                    .GroupBy(r => r.UpdatedBy.Value)
                    .Select(g => new
                    {
                        StaffId = g.Key,
                        TotalCount = g.Count()
                    })
                    .ToListAsync();

                // 👥 Get staff names
                var staffInfo = await _context.Staff
                    .Where(s => staffIds.Contains(s.Id))
                    .Select(s => new
                    {
                        s.Id,
                        FullName = (s.FirstName + " " + s.LastName).Trim()
                    })
                    .ToListAsync();

                var results = new List<AnalyticsBaseDTO>();

                // 🧠 Compute totals or averages
                foreach (var staffId in staffIds)
                {
                    var staffData = groupedByStaff.FirstOrDefault(g => g.StaffId == staffId);
                    double totalCount = staffData?.TotalCount ?? 0;

                    double value;
                    if (isAverage)
                    {
                        double intervalCount = interval switch
                        {
                            IntervalType.Day => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays, 1),
                            IntervalType.Week => Math.Max((effectiveEnd.Date - effectiveStart.Date).TotalDays / 7.0, 1),
                            IntervalType.Month => Math.Max(((effectiveEnd.Year - effectiveStart.Year) * 12) +
                                                           (effectiveEnd.Month - effectiveStart.Month) + 1, 1),
                            IntervalType.Hour => Math.Max((effectiveEnd - effectiveStart).TotalHours, 1),
                            _ => 1
                        };

                        value = Math.Round(totalCount / intervalCount, 2);
                    }
                    else
                    {
                        value = totalCount;
                    }

                    var staff = staffInfo.FirstOrDefault(s => s.Id == staffId);
                    string name = staff?.FullName ?? "Unknown Staff";

                    results.Add(new AnalyticsBaseDTO
                    {
                        Id = staffId.ToString(),
                        Name = name,
                        Value = value
                    });
                }

                var dto = new OrdersPerStaffDTO
                {
                    Analytics = results.OrderBy(r => r.Name).ToList()
                };

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = dto
                };
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


        // public async Task<SystemResponse> RollsPerStaffInTimeframe(
        //     List<Guid>? staffIds,
        //     DateTime? startDate,
        //     DateTime? endDate,
        //     bool isAverage,
        //     IntervalType? interval)
        // {
        //     try
        //     {
        //         DateTime? utcStart = startDate?.ToUniversalTime();
        //         DateTime? utcEnd = endDate?.ToUniversalTime();

        //         var rollDatesSorted = _context.Rolls.OrderBy(o => o.CreatedBy)
        //             .Select(o => o.DateCreated);

        //         DateTime? firstRollDate = await rollDatesSorted.FirstOrDefaultAsync();
        //         // DateTime? lastRollDate = await rollDatesSorted.LastOrDefaultAsync();

        //         DateTime effectiveStart = startDate ?? firstRollDate ?? DateTime.Today;
        //         DateTime effectiveEnd = endDate ?? DateTime.Today.AddDays(1);

        //         DateTime localeffectiveStart = startDate ?? firstRollDate ?? (DateTime.Today);
        //         DateTime localeffectiveEnd = endDate ?? (DateTime.Today.AddDays(1));

        //         var query = _context.Rolls
        //             .AsNoTracking()
        //             .Where(r => r.Status == Enums.RollStatus.Processed)
        //             .Where(r => (staffIds != null && staffIds.Any()) ? staffIds.Contains(r.UpdatedBy.Value) : true)
        //             .Where(r => (!utcStart.HasValue || r.DateUpdated.Value >= utcStart)
        //                      && (!utcEnd.HasValue || r.DateUpdated.Value <= utcEnd));

        //         if (!isAverage)
        //         {
        //             var total = await query.CountAsync();
        //             return new SystemResponse
        //             {
        //                 IsSuccess = true,
        //                 ReturnObject = total
        //             };
        //         }

        //         interval ??= IntervalType.Day;
        //         var duration = effectiveEnd - effectiveStart;

        //         ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

        //         List<GroupedRollResult> groupedWithZeros;

        //         if (interval == IntervalType.Week)
        //         {
        //             var rollDates = await query
        //                 .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
        //                 .ToListAsync();

        //             var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
        //                 .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .Select(g => new
        //                 {
        //                     g.Key.Year,
        //                     g.Key.Week,
        //                     Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
        //                 })
        //                 .ToList();

        //             var rollGroups = rollDates
        //                 .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
        //                 .ToDictionary(
        //                     g => $"Week {g.Key.Week:00} ({g.Key.Year})",
        //                     g => g.Count()
        //                 );

        //             groupedWithZeros = allWeekDates
        //                 .Select(w => new GroupedRollResult
        //                 {
        //                     Interval = w.Label,
        //                     Count = rollGroups.TryGetValue(w.Label, out int count) ? count : 0
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Day)
        //         {
        //             int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
        //             var allDates = Enumerable.Range(0, totalDays)
        //                 .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allDates
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Hour)
        //         {
        //             int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
        //             var allHours = Enumerable.Range(0, totalHours)
        //                 .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month,
        //                     r.DateUpdated.Value.Day,
        //                     r.DateUpdated.Value.Hour
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allHours
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM-dd HH:00");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else if (interval == IntervalType.Month)
        //         {
        //             DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
        //             DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
        //             int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

        //             var allMonths = Enumerable.Range(0, totalMonths)
        //                 .Select(i => firstMonth.AddMonths(i))
        //                 .ToList();

        //             var grouped = await query
        //                 .GroupBy(r => new
        //                 {
        //                     r.DateUpdated.Value.Year,
        //                     r.DateUpdated.Value.Month
        //                 })
        //                 .Select(g => new GroupedRollResult
        //                 {
        //                     Interval = $"{g.Key.Year}-{g.Key.Month:00}",
        //                     Count = g.Count()
        //                 })
        //                 .ToListAsync();

        //             groupedWithZeros = allMonths
        //                 .Select(date =>
        //                 {
        //                     string label = date.ToString("yyyy-MM");
        //                     var existing = grouped.FirstOrDefault(g => g.Interval == label);
        //                     return new GroupedRollResult
        //                     {
        //                         Interval = label,
        //                         Count = existing?.Count ?? 0
        //                     };
        //                 })
        //                 .ToList();
        //         }
        //         else
        //         {
        //             return new SystemResponse
        //             {
        //                 IsSuccess = false,
        //                 Message = "Unsupported interval type."
        //             };
        //         }

        //         double average = groupedWithZeros.Any()
        //             ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
        //             : 0;

        //         return new SystemResponse
        //         {
        //             IsSuccess = true,
        //             ReturnObject = average
        //         };
        //     }
        //     catch (Exception ex)
        //     {
        //         return new SystemResponse
        //         {
        //             IsSuccess = false,
        //             Message = ex.Message,
        //             ReturnObject = ex
        //         };
        //     }
        // }

        private void ValidateInterval(DateTime start, DateTime end, IntervalType interval)
        {
            var duration = end - start;

            switch (interval)
            {
                case IntervalType.Month when duration.TotalDays < 28:
                    throw new ArgumentException("A 'Month' interval requires at least 28 days.");
                case IntervalType.Week when duration.TotalDays < 7:
                    throw new ArgumentException("A 'Week' interval requires at least 7 days.");
                case IntervalType.Day when duration.TotalDays < 1:
                    throw new ArgumentException("A 'Day' interval requires at least 1 day.");
                case IntervalType.Hour when duration.TotalHours < 1:
                    throw new ArgumentException("An 'Hour' interval requires at least 1 hour.");
            }
        }

        private class GroupedRollResult
        {
            public string Interval { get; set; }
            public int Count { get; set; }
        }
    }
}
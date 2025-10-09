using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Libs.Classes;
using Libs.Data.Context;
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
            IntervalType? interval
        )
        {
            try
            {
                DateTime? utcStart = startDate?.ToUniversalTime();
                DateTime? utcEnd = endDate?.ToUniversalTime();

                DateTime effectiveStart = startDate ?? DateTime.MinValue;
                DateTime effectiveEnd = endDate ?? DateTime.MaxValue;

                DateTime localeffectiveStart = startDate ?? DateTime.MinValue;
                DateTime localeffectiveEnd = endDate ?? DateTime.MaxValue;

                var query = _context.Orders
                    .Include(o => o.Scanner)
                    .AsNoTracking()
                    .Where(o => o.Status == Enums.OrderStatus.Completed)
                    .Where(o => (scannerIds != null && scannerIds.Any()) ? scannerIds.Contains(o.Scanner.Id) : true)
                    .Where(o => (!utcStart.HasValue || o.DateUpdated.Value >= utcStart)
                             && (!utcEnd.HasValue || o.DateUpdated.Value <= utcEnd));

                if (!isAverage)
                {
                    var total = await query.CountAsync();
                    return new SystemResponse
                    {
                        IsSuccess = true,
                        ReturnObject = total
                    };
                }

                interval ??= IntervalType.Day;
                var duration = effectiveEnd - effectiveStart;

                ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

                List<GroupedRollResult> groupedWithZeros;

                if (interval == IntervalType.Week)
                {
                    var orderDates = await query
                        .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
                        .ToListAsync();

                    var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
                        .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Week,
                            Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
                        })
                        .ToList();

                    var orderGroups = orderDates
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .ToDictionary(
                            g => $"Week {g.Key.Week:00} ({g.Key.Year})",
                            g => g.Count()
                        );

                    groupedWithZeros = allWeekDates
                        .Select(w => new GroupedRollResult
                        {
                            Interval = w.Label,
                            Count = orderGroups.TryGetValue(w.Label, out int count) ? count : 0
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Day)
                {
                    int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
                    var allDates = Enumerable.Range(0, totalDays)
                        .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allDates
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Hour)
                {
                    int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
                    var allHours = Enumerable.Range(0, totalHours)
                        .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day,
                            r.DateUpdated.Value.Hour
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allHours
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd HH:00");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Month)
                {
                    DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
                    DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
                    int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

                    var allMonths = Enumerable.Range(0, totalMonths)
                        .Select(i => firstMonth.AddMonths(i))
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allMonths
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Unsupported interval type."
                    };
                }

                double average = groupedWithZeros.Any()
                    ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
                    : 0;

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = average
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

        public async Task<SystemResponse> OrdersPerStaffInTimeframe(
            List<Guid>? staffIds,
            DateTime? startDate,
            DateTime? endDate,
            bool isAverage,
            IntervalType? interval
        )
        {
            try
            {
                DateTime? utcStart = startDate?.ToUniversalTime();
                DateTime? utcEnd = endDate?.ToUniversalTime();

                DateTime effectiveStart = startDate ?? DateTime.MinValue;
                DateTime effectiveEnd = endDate ?? DateTime.MaxValue;

                DateTime localeffectiveStart = startDate ?? DateTime.MinValue;
                DateTime localeffectiveEnd = endDate ?? DateTime.MaxValue;

                var query = _context.Orders
                    .AsNoTracking()
                    .Where(r => r.Status == Enums.OrderStatus.Completed)
                    .Where(r => (staffIds != null && staffIds.Any()) ? staffIds.Contains(r.UpdatedBy.Value) : true)
                    .Where(r => (!utcStart.HasValue || r.DateUpdated.Value >= utcStart)
                             && (!utcEnd.HasValue || r.DateUpdated.Value <= utcEnd));

                if (!isAverage)
                {
                    var total = await query.CountAsync();
                    return new SystemResponse
                    {
                        IsSuccess = true,
                        ReturnObject = total
                    };
                }

                interval ??= IntervalType.Day;
                var duration = effectiveEnd - effectiveStart;

                ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

                List<GroupedRollResult> groupedWithZeros;

                if (interval == IntervalType.Week)
                {
                    var orderDates = await query
                        .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
                        .ToListAsync();

                    var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
                        .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Week,
                            Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
                        })
                        .ToList();

                    var orderGroups = orderDates
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .ToDictionary(
                            g => $"Week {g.Key.Week:00} ({g.Key.Year})",
                            g => g.Count()
                        );

                    groupedWithZeros = allWeekDates
                        .Select(w => new GroupedRollResult
                        {
                            Interval = w.Label,
                            Count = orderGroups.TryGetValue(w.Label, out int count) ? count : 0
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Day)
                {
                    int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
                    var allDates = Enumerable.Range(0, totalDays)
                        .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allDates
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Hour)
                {
                    int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
                    var allHours = Enumerable.Range(0, totalHours)
                        .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day,
                            r.DateUpdated.Value.Hour
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allHours
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd HH:00");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Month)
                {
                    DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
                    DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
                    int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

                    var allMonths = Enumerable.Range(0, totalMonths)
                        .Select(i => firstMonth.AddMonths(i))
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allMonths
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Unsupported interval type."
                    };
                }

                double average = groupedWithZeros.Any()
                    ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
                    : 0;

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = average
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

        public async Task<SystemResponse> RollsPerScannerInTimeframe(
            List<Guid>? scannerIds,
            DateTime? startDate,
            DateTime? endDate,
            bool isAverage,
            IntervalType? interval)
        {
            try
            {
                DateTime? utcStart = startDate?.ToUniversalTime();
                DateTime? utcEnd = endDate?.ToUniversalTime();

                DateTime effectiveStart = startDate ?? DateTime.MinValue;
                DateTime effectiveEnd = endDate ?? DateTime.MaxValue;

                DateTime localeffectiveStart = startDate ?? DateTime.MinValue;
                DateTime localeffectiveEnd = endDate ?? DateTime.MaxValue;

                var query = _context.Rolls
                    .Include(r => r.Order)
                    .Include(r => r.Order.Scanner)
                    .AsNoTracking()
                    .Where(r => r.Status == Enums.RollStatus.Processed)
                    .Where(r => (scannerIds != null && scannerIds.Any()) ? scannerIds.Contains(r.Order.Scanner.Id) : true)
                    .Where(r => (!utcStart.HasValue || r.DateUpdated.Value >= utcStart)
                             && (!utcEnd.HasValue || r.DateUpdated.Value <= utcEnd));

                if (!isAverage)
                {
                    var total = await query.CountAsync();
                    return new SystemResponse
                    {
                        IsSuccess = true,
                        ReturnObject = total
                    };
                }

                interval ??= IntervalType.Day;
                var duration = effectiveEnd - effectiveStart;

                ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

                List<GroupedRollResult> groupedWithZeros;

                if (interval == IntervalType.Week)
                {
                    var rollDates = await query
                        .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
                        .ToListAsync();

                    var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
                        .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Week,
                            Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
                        })
                        .ToList();

                    var rollGroups = rollDates
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .ToDictionary(
                            g => $"Week {g.Key.Week:00} ({g.Key.Year})",
                            g => g.Count()
                        );

                    groupedWithZeros = allWeekDates
                        .Select(w => new GroupedRollResult
                        {
                            Interval = w.Label,
                            Count = rollGroups.TryGetValue(w.Label, out int count) ? count : 0
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Day)
                {
                    int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
                    var allDates = Enumerable.Range(0, totalDays)
                        .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allDates
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Hour)
                {
                    int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
                    var allHours = Enumerable.Range(0, totalHours)
                        .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day,
                            r.DateUpdated.Value.Hour
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allHours
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd HH:00");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Month)
                {
                    DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
                    DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
                    int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

                    var allMonths = Enumerable.Range(0, totalMonths)
                        .Select(i => firstMonth.AddMonths(i))
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allMonths
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Unsupported interval type."
                    };
                }

                double average = groupedWithZeros.Any()
                    ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
                    : 0;

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = average
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

        public async Task<SystemResponse> RollsPerStaffInTimeframe(
            List<Guid>? staffIds,
            DateTime? startDate,
            DateTime? endDate,
            bool isAverage,
            IntervalType? interval)
        {
            try
            {
                DateTime? utcStart = startDate?.ToUniversalTime();
                DateTime? utcEnd = endDate?.ToUniversalTime();

                DateTime effectiveStart = startDate ?? DateTime.MinValue;
                DateTime effectiveEnd = endDate ?? DateTime.MaxValue;

                DateTime localeffectiveStart = startDate ?? DateTime.MinValue;
                DateTime localeffectiveEnd = endDate ?? DateTime.MaxValue;

                var query = _context.Rolls
                    .AsNoTracking()
                    .Where(r => r.Status == Enums.RollStatus.Processed)
                    .Where(r => (staffIds != null && staffIds.Any()) ? staffIds.Contains(r.UpdatedBy.Value) : true)
                    .Where(r => (!utcStart.HasValue || r.DateUpdated.Value >= utcStart)
                             && (!utcEnd.HasValue || r.DateUpdated.Value <= utcEnd));

                if (!isAverage)
                {
                    var total = await query.CountAsync();
                    return new SystemResponse
                    {
                        IsSuccess = true,
                        ReturnObject = total
                    };
                }

                interval ??= IntervalType.Day;
                var duration = effectiveEnd - effectiveStart;

                ValidateInterval(effectiveStart, effectiveEnd, interval.Value);

                List<GroupedRollResult> groupedWithZeros;

                if (interval == IntervalType.Week)
                {
                    var rollDates = await query
                        .Select(r => r.DateUpdated.Value.ToUniversalTime().Date)
                        .ToListAsync();

                    var allWeekDates = Enumerable.Range(0, (effectiveEnd.Date - effectiveStart.Date).Days + 1)
                        .Select(offset => effectiveStart.Date.AddDays(offset).ToUniversalTime())
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Week,
                            Label = $"Week {g.Key.Week:00} ({g.Key.Year})"
                        })
                        .ToList();

                    var rollGroups = rollDates
                        .GroupBy(d => new { d.Year, Week = ISOWeek.GetWeekOfYear(d) })
                        .ToDictionary(
                            g => $"Week {g.Key.Week:00} ({g.Key.Year})",
                            g => g.Count()
                        );

                    groupedWithZeros = allWeekDates
                        .Select(w => new GroupedRollResult
                        {
                            Interval = w.Label,
                            Count = rollGroups.TryGetValue(w.Label, out int count) ? count : 0
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Day)
                {
                    int totalDays = (int)(effectiveEnd.Date - effectiveStart.Date).TotalDays + 1;
                    var allDates = Enumerable.Range(0, totalDays)
                        .Select(i => effectiveStart.Date.AddDays(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allDates
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Hour)
                {
                    int totalHours = (int)(effectiveEnd - effectiveStart).TotalHours + 1;
                    var allHours = Enumerable.Range(0, totalHours)
                        .Select(i => effectiveStart.AddHours(i).ToUniversalTime())
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month,
                            r.DateUpdated.Value.Day,
                            r.DateUpdated.Value.Hour
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} {g.Key.Hour:00}:00",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allHours
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM-dd HH:00");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else if (interval == IntervalType.Month)
                {
                    DateTime firstMonth = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);
                    DateTime lastMonth = new DateTime(effectiveEnd.Year, effectiveEnd.Month, 1);
                    int totalMonths = ((lastMonth.Year - firstMonth.Year) * 12) + (lastMonth.Month - firstMonth.Month) + 1;

                    var allMonths = Enumerable.Range(0, totalMonths)
                        .Select(i => firstMonth.AddMonths(i))
                        .ToList();

                    var grouped = await query
                        .GroupBy(r => new
                        {
                            r.DateUpdated.Value.Year,
                            r.DateUpdated.Value.Month
                        })
                        .Select(g => new GroupedRollResult
                        {
                            Interval = $"{g.Key.Year}-{g.Key.Month:00}",
                            Count = g.Count()
                        })
                        .ToListAsync();

                    groupedWithZeros = allMonths
                        .Select(date =>
                        {
                            string label = date.ToString("yyyy-MM");
                            var existing = grouped.FirstOrDefault(g => g.Interval == label);
                            return new GroupedRollResult
                            {
                                Interval = label,
                                Count = existing?.Count ?? 0
                            };
                        })
                        .ToList();
                }
                else
                {
                    return new SystemResponse
                    {
                        IsSuccess = false,
                        Message = "Unsupported interval type."
                    };
                }

                double average = groupedWithZeros.Any()
                    ? Math.Round(groupedWithZeros.Average(x => x.Count), 2)
                    : 0;

                return new SystemResponse
                {
                    IsSuccess = true,
                    ReturnObject = average
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Helpers
{
    public static class DateTimeHelpers
    {
        public static DateTime? GetMondayOfWeek(DateTime date)
        {
            try
            {
                int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
                return date.Date.AddDays(-diff);
            }
            catch
            {
                return null;
            }

        }
    }
}
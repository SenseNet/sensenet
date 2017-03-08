using System;
using System.Globalization;

namespace SenseNet.ContentRepository
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime date)
        {
            var diff = date.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            if (diff < 0)
                diff += 7;

            return date.AddDays(-1 * diff).Date;
        }

        public static DateTime AddWorkdays(this DateTime date, int value)
        {
            var nwd = date.Date;
            var operand = value < 0 ? -1 : 1;
            var daysToAdd = value;

            while (daysToAdd != 0)
            {
                // skip weekends
                while (nwd.DayOfWeek == DayOfWeek.Saturday || nwd.DayOfWeek == DayOfWeek.Sunday)
                {
                    nwd = nwd.AddDays(operand);
                }

                // move to the next day
                nwd = nwd.AddDays(operand);

                // +1 or -1
                daysToAdd += -operand;
            }

            return nwd;
        }

        public static string ToContentQueryString(this DateTime date)
        {
            return "'" + date.ToString(CultureInfo.InvariantCulture) + "'";
        }
    }
}

using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SenseNet.ContentRepository.Storage.Schema
{
    internal class TypeConverter
    {
        internal static int ToInt32(object value)
        {
            return ((value == null || value == System.DBNull.Value) ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        internal static string ToString(object value)
        {
            return ((value == null || value == System.DBNull.Value) ? string.Empty : Convert.ToString(value));
        }

        internal static bool ToBoolean(object value)
        {
            return ((value == null || value == System.DBNull.Value) ? false : ToInt32(value) != 0);
        }

        internal static DateTime ToDateTime(object value)
        {
            return DateTime.SpecifyKind(((value == null || value == System.DBNull.Value) ? DateTime.MinValue : Convert.ToDateTime(value)), DateTimeKind.Utc);
        }

        internal static decimal ToDecimal(object value)
        {
            return ((value == null || value == System.DBNull.Value) ? 0 : Convert.ToDecimal(value));
        }

        internal static long ToLong(object value)
        {
            return ((value == null || value == System.DBNull.Value) ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
    }
}
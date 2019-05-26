using System;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Helper methods for converting database values to .Net types safely, taking DbNull into account.
    /// </summary>
    public static class DataReaderExtension
    {
        /// <summary>
        /// Converts a byte DB column value to a .NET byte value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static byte GetSafeByte(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (byte)0 : reader.GetByte(index);
        }
        /// <summary>
        /// Converts an Int64 DB column value to a .NET long value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static long GetSafeInt64(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
        }
        /// <summary>
        /// Converts an Int32 DB column value to a .NETn integer value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static int GetSafeInt32(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt32(index);
        }
        /// <summary>
        /// Converts an Int16 DB column value to a .NET short value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static short GetSafeInt16(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (short)0 : reader.GetInt16(index);
        }
        /// <summary>
        /// Converts a byte DB column value to a .NET bool value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static bool GetSafeBooleanFromByte(this IDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetByte(index));
        }
        /// <summary>
        /// Converts a Boolean DB column value to a .NET bool value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static bool GetSafeBooleanFromBoolean(this IDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && reader.GetBoolean(index);
        }
        /// <summary>
        /// Converts a DateTime DB column value to a .NET DateTime value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static DateTime? GetSafeDateTime(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (DateTime?)null : reader.GetDateTime(index);
        }
        /// <summary>
        /// Converts a String DB column value to a .NET string value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static string GetSafeString(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : reader.GetString(index);
        }
        /// <summary>
        /// Converts an array of bytes DB column value to a .NET long value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static long GetSafeLongFromBytes(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0L;

            return Tools.Utility.Convert.BytesToLong((byte[]) reader[index]);
        }

        public static DateTime GetDateTimeUtc(this IDataReader reader, int ordinal)
        {
            DateTime unspecified = reader.GetDateTime(ordinal);
            return DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
        }

        /* ============================================================================= */

        public static byte GetByte(this IDataReader reader, string columnName)
        {
            return reader.GetByte(reader.GetOrdinal(columnName));
        }
        public static byte GetSafeByte(this IDataReader reader, string columnName)
        {
            return reader.GetSafeByte(reader.GetOrdinal(columnName));
        }
        public static short GetInt16(this IDataReader reader, string columnName)
        {
            return reader.GetInt16(reader.GetOrdinal(columnName));
        }
        public static short GetSafeInt16(this IDataReader reader, string columnName)
        {
            return reader.GetSafeInt16(reader.GetOrdinal(columnName));
        }
        public static int GetInt32(this IDataReader reader, string columnName)
        {
            return reader.GetInt32(reader.GetOrdinal(columnName));
        }
        public static int GetSafeInt32(this IDataReader reader, string columnName)
        {
            return reader.GetSafeInt32(reader.GetOrdinal(columnName));
        }
        public static bool GetSafeBooleanFromByte(this IDataReader reader, string columnName)
        {
            return reader.GetSafeBooleanFromByte(reader.GetOrdinal(columnName));
        }
        public static string GetString(this IDataReader reader, string columnName)
        {
            return reader.GetString(reader.GetOrdinal(columnName));
        }
        public static string GetSafeString(this IDataReader reader, string columnName)
        {
            return reader.GetSafeString(reader.GetOrdinal(columnName));
        }
        public static long GetSafeLongFromBytes(this IDataReader reader, string columnName)
        {
            return reader.GetSafeLongFromBytes(reader.GetOrdinal(columnName));
        }

        public static DateTime GetDateTimeUtc(this IDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.GetDateTimeUtc(ordinal);
        }

        public static T GetEnumValueByName<T>(this IDataReader reader, string name)
        {
            var value = reader.GetString(name);
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}

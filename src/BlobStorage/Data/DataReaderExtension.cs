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
        /// Converts an Int64 db column value to a long value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static long GetSafeInt64(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
        }
        /// <summary>
        /// Converts an Int32 db column value to an integer value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static int GetSafeInt32(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt32(index);
        }
        /// <summary>
        /// Converts an Int16 db column value to a short value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static short GetSafeInt16(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (short)0 : reader.GetInt16(index);
        }
        /// <summary>
        /// Converts a byte db column value to a bool value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static bool GetSafeBooleanFromByte(this IDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetByte(index));
        }
        /// <summary>
        /// Converts a Boolean db column value to a bool value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static bool GetSafeBooleanFromBoolean(this IDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && reader.GetBoolean(index);
        }
        /// <summary>
        /// Converts a String db column value to a string value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static string GetSafeString(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : reader.GetString(index);
        }
        /// <summary>
        /// Converts an array of bytes db column value to a long value safely.
        /// </summary>
        /// <param name="reader">Data reader pointing to a record that contains a column to be converted.</param>
        /// <param name="index">The index of the column to find.</param>
        public static long GetSafeLongFromBytes(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0L;

            return Tools.Utility.Convert.BytesToLong((byte[]) reader[index]);
        }
    }
}

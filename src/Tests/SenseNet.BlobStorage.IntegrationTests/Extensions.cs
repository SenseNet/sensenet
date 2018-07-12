using System;
using System.Data;

namespace SenseNet.BlobStorage.IntegrationTests
{
    internal static class Extensions
    {
        public static int ToInt(this long input)
        {
            return Convert.ToInt32(input);
        }


        public static long GetSafeInt64(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
        }
        public static int GetSafeInt32(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : reader.GetInt32(index);
        }
        public static DateTime? GetSafeDateTime(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (DateTime?)null : reader.GetDateTime(index);
        }
        public static string GetSafeString(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : reader.GetString(index);
        }
        public static bool GetSafeBoolFromBit(this IDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && reader.GetBoolean(index);
        }
        public static byte[] GetSafeBytes(this IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : (byte[])reader[index];
        }
    }
}

//using System;
//using System.Data;

//namespace SenseNet.ContentRepository.Storage.Data.SqlClient
//{
//    public static class DataReaderExtensions
//    {
//        public static DateTime GetDateTimeUtc(this IDataReader reader, string name)
//        {
//            int ordinal = reader.GetOrdinal(name);
//            return reader.GetDateTimeUtc(ordinal);
//        }

//        public static DateTime GetDateTimeUtc(this IDataReader reader, int ordinal)
//        {
//            DateTime unspecified = reader.GetDateTime(ordinal);
//            return DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
//        }
//    }
//}

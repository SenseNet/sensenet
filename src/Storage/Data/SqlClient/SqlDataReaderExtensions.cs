using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public static class SqlDataReaderExtensions
    {
        public static DateTime GetDateTimeUtc(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.GetDateTimeUtc(ordinal);
        }

        public static DateTime GetDateTimeUtc(this SqlDataReader reader, int ordinal)
        {
            DateTime unspecified = reader.GetDateTime(ordinal);
            return DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
        }
    }  
}

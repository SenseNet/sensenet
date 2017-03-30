using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    //UNDONE: rename to DataReaderExtensions
    public static class DataReaderExtensions
    {
        public static DateTime GetDateTimeUtc(this IDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.GetDateTimeUtc(ordinal);
        }

        public static DateTime GetDateTimeUtc(this IDataReader reader, int ordinal)
        {
            DateTime unspecified = reader.GetDateTime(ordinal);
            return DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
        }
    }  
}

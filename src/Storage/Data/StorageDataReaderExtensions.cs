using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Storage.Data
{
    public static class StorageDataReaderExtensions
    {
        internal static ContentSavingState GetSavingState(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0;
            return (ContentSavingState)reader.GetInt32(index);
        }
        public static IEnumerable<ChangedData> GetChangedData(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;
            var src = reader.GetString(index);
            var data = (IEnumerable<ChangedData>)JsonConvert.DeserializeObject(src, typeof(IEnumerable<ChangedData>));
            return data;
        }

        /* ============================================================================= */

        public static ContentSavingState GetSavingState(this IDataReader reader, string columnName)
        {
            return reader.GetSavingState(reader.GetOrdinal(columnName));

        }
        public static IEnumerable<ChangedData> GetChangedData(this IDataReader reader, string columnName)
        {
            return reader.GetChangedData(reader.GetOrdinal(columnName));
        }
    }
}

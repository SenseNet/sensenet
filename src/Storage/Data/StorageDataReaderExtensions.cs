using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Storage.Data
{
    internal static class StorageDataReaderExtensions
    {
        internal static ContentSavingState GetSavingState(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0;
            return (ContentSavingState)reader.GetInt32(index);
        }
        internal static IEnumerable<ChangedData> GetChangedData(this IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;
            var src = reader.GetString(index);
            var data = (IEnumerable<ChangedData>)JsonConvert.DeserializeObject(src, typeof(IEnumerable<ChangedData>));
            return data;
        }
    }
}

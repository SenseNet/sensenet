using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    //UNDONE:<?xxx: Delete if all references rewritten in the ecosystem
    [Obsolete("Do not use this tool anymore", true)]
    public static class IndexingTools
    {
        [Obsolete("Use IIndexManager.", true)]
        public static void AddTextExtract(int versionId, string textExtract)
        {
            Providers.Instance.IndexManager.AddTextExtract(versionId, textExtract);
        }

        [Obsolete("Use ContentTypeManager.", true)]
        public static IEnumerable<string> GetAllFieldNames(bool includeNonIndexedFields = true)
        {
            return ContentTypeManager.GetAllFieldNames(includeNonIndexedFields);
        }

        [Obsolete("Use ContentTypeManager.", true)]
        public static IEnumerable<ExplicitPerFieldIndexingInfo> GetExplicitPerFieldIndexingInfo(bool includeNonIndexedFields)
        {
            return ContentTypeManager.GetExplicitPerFieldIndexingInfo(includeNonIndexedFields);
        }

        [Obsolete("Use ContentTypeManager.", true)]
        public static string GetExplicitIndexingInfo(bool fullTable)
        {
            return ContentTypeManager.GetExplicitIndexingInfo(fullTable);
        }
    }
}

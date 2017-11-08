using System;
using System.Collections.Generic;
using System.IO;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Search.Indexing
{
    public enum IndexRebuildLevel { IndexOnly, DatabaseAndIndex };

    public interface IIndexPopulator
    {
        void ClearAndPopulateAll(TextWriter consoleWriter = null);
        void RepopulateTree(string newPath);
        void PopulateTree(string newPath, int nodeId);
        object BeginPopulateNode(Node node, NodeSaveSettings settings, string originalPath, string newPath);
        void CommitPopulateNode(object data, IndexDocumentData indexDocument);
        void FinalizeTextExtracting(object data, IndexDocumentData indexDocument);
        void DeleteTree(string path, int nodeId);
        void DeleteForest(IEnumerable<int> idSet);
        void DeleteForest(IEnumerable<string> pathSet);

        void RebuildIndex(Node node, bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly);

        event EventHandler<NodeIndexedEventArgs> NodeIndexed;
    }
}

using System.Collections.Generic;
using System.IO;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Indexing
{
    public interface IIndexingEngine
    {
        bool Running { get; }
        bool IndexIsCentralized { get; }

        void Start(TextWriter consoleOut);
        void ShutDown();
        void ClearIndex();

        IndexingActivityStatus ReadActivityStatusFromIndex();
        void WriteActivityStatusToIndex(IndexingActivityStatus state);

        void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> addition);
    }
}

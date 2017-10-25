using System.Collections.Generic;
using System.IO;

namespace SenseNet.Search
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

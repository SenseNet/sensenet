using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Tests.Implementations
{
    public class InMemoryIndexingEngine : IIndexingEngine
    {
        public InMemoryIndex Index { get; } = InMemoryIndex.Create();

        public bool Running { get; private set; }

        public bool IndexIsCentralized => false;

        public void Start(TextWriter consoleOut)
        {
            Trace.WriteLine($"TMPINVEST: Starting inmemory indexing engine.");
            Running = true;
        }

        public void ShutDown()
        {
            Trace.WriteLine($"TMPINVEST: Shutting down indexing engine.");
            Running = false;
        }

        public void ClearIndex()
        {
            Index.Clear();
        }

        public IndexingActivityStatus ReadActivityStatusFromIndex()
        {
            return Index.ReadActivityStatus();
        }

        public void WriteActivityStatusToIndex(IndexingActivityStatus state)
        {
            Index.WriteActivityStatus(state);
        }

        public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions)
        {
            if (deletions != null)
                foreach (var term in deletions)
                    Index.Delete(term);

            if (updates != null)
                foreach (var update in updates)
                    Index.Update(update.UpdateTerm, update.Document);

            if (additions != null)
                foreach(var doc in additions)
                    Index.AddDocument(doc);
        }
    }
}

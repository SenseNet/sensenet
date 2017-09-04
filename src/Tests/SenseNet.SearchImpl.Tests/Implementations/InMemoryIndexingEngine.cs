using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class InMemoryIndexingEngineFactory : IIndexingEngineFactory
    {
        //TODO: thread affinity to enable multithreaded unit testing
        public InMemoryIndexingEngine Instance { get; } = new InMemoryIndexingEngine();

        public IIndexingEngine CreateIndexingEngine()
        {
            return Instance;
        }
    }

    internal class InMemoryIndexingEngine : IIndexingEngine
    {
        internal InMemoryIndex Index { get; } = new InMemoryIndex();

        public bool Running { get; private set; }

        public void Start(TextWriter consoleOut)
        {
            IndexingActivityQueue.Startup(consoleOut);
            Running = true;
        }

        public void ShutDown()
        {
            Running = false;
        }

        public void ActivityFinished()
        {
            // do nothing
        }

        public void Commit(int lastActivityId = 0)
        {
            // do nothing
        }

        public void ClearIndex()
        {
            Index.Clear();
        }

        public IIndexingActivityStatus ReadActivityStatusFromIndex()
        {
            return IndexingActivityStatus.Startup;
        }

        public IEnumerable<IndexDocument> GetDocumentsByNodeId(int nodeId)
        {
            throw new NotImplementedException();
        }

        public void Actualize(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates)
        {
            if (deletions != null)
                foreach (var term in deletions)
                    Index.Delete(term);

            if (updates != null)
                foreach (var update in updates)
                    Index.Update(update.UpdateTerm, (IndexDocument)update.Document);

            if (addition != null)
                Index.AddDocument(addition);
        }

        public void Actualize(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> addition)
        {
            if (deletions != null)
                foreach (var term in deletions)
                    Index.Delete(term);

            if (addition != null)
                foreach(var doc in addition)
                    Index.AddDocument(doc);
        }
    }
}

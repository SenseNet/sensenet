using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Tests.Implementations
{
    public class InMemoryIndexingEngine : IIndexingEngine
    {
        private readonly InMemorySearchEngine _searchEngine;
        private InMemoryIndex Index => _searchEngine.Index;

        public bool Running { get; private set; }

        public bool IndexIsCentralized => false;

        public InMemoryIndexingEngine(InMemorySearchEngine searchEngine)
        {
            _searchEngine = searchEngine;
        }

        public Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
        {
            Running = true;
            return Task.CompletedTask;
        }

        public Task ShutDownAsync(CancellationToken cancellationToken)
        {
            Running = false;
            return Task.CompletedTask;
        }

        public Task ClearIndexAsync(CancellationToken cancellationToken)
        {
            Index.Clear();
            return Task.CompletedTask;
        }

        public Task<IndexingActivityStatus> ReadActivityStatusFromIndexAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Index.ReadActivityStatus());
        }

        public Task WriteActivityStatusToIndexAsync(IndexingActivityStatus state, CancellationToken cancellationToken)
        {
            Index.WriteActivityStatus(state);
            return Task.CompletedTask;
        }

        public Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, 
            IEnumerable<IndexDocument> additions, CancellationToken cancellationToken)
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

            return Task.CompletedTask;
        }
    }
}

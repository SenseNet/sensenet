using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.InMemory
{
    public class InMemoryIndexingEngine : IIndexingEngine
    {
        private readonly InMemorySearchEngine _searchEngine;
        private InMemoryIndex Index => _searchEngine.Index;

        public bool Running { get; private set; }

        public bool IndexIsCentralized { get; set; } = false;

        public InMemoryIndexingEngine(InMemorySearchEngine searchEngine)
        {
            _searchEngine = searchEngine;
        }

        public STT.Task StartAsync(TextWriter consoleOut, CancellationToken cancellationToken)
        {
            Running = true;
            return STT.Task.CompletedTask;
        }

        public STT.Task ShutDownAsync(CancellationToken cancellationToken)
        {
            Running = false;
            return STT.Task.CompletedTask;
        }

        public STT.Task<IndexBackupResult> BackupAsync(CancellationToken cancellationToken)
        {
            throw new SnNotSupportedException();
        }

        public STT.Task<IndexBackupResult> BackupAsync(string target, CancellationToken cancellationToken)
        {
            throw new SnNotSupportedException();
        }

        public STT.Task ClearIndexAsync(CancellationToken cancellationToken)
        {
            Index.Clear();
            return STT.Task.CompletedTask;
        }

        public Task<IndexingActivityStatus> ReadActivityStatusFromIndexAsync(CancellationToken cancellationToken)
        {
            return STT.Task.FromResult(Index.ReadActivityStatus());
        }

        public STT.Task WriteActivityStatusToIndexAsync(IndexingActivityStatus state, CancellationToken cancellationToken)
        {
            Index.WriteActivityStatus(state);
            return STT.Task.CompletedTask;
        }

        public STT.Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, 
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

            return STT.Task.CompletedTask;
        }
    }
}

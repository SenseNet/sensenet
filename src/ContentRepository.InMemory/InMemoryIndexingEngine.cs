using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        internal List<string> NumberFields { get; set; }

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

        public STT.Task<BackupResponse> BackupAsync(string target, CancellationToken cancellationToken)
        {
            throw new SnNotSupportedException();
        }

        public Task<BackupResponse> QueryBackupAsync(CancellationToken cancellationToken)
        {
            throw new SnNotSupportedException();
        }

        public Task<BackupResponse> CancelBackupAsync(CancellationToken cancellationToken)
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

        public virtual STT.Task WriteIndexAsync(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, 
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

        public IndexProperties GetIndexProperties()
        {
            return new IndexProperties
            {
                IndexingActivityStatus = Index.ReadActivityStatus(),
                FieldInfo = Index.IndexData
                    .ToDictionary(x => x.Key, x => x.Value.Count)
                    .OrderBy(x=>x.Key)
                    .ToDictionary(x=>x.Key, x=>x.Value),
                VersionIds = Index.IndexData
                    .SelectMany(x => x.Value
                        .SelectMany(y => y.Value))
                    .Distinct()
                    .OrderBy(z => z)
                    .ToArray()
            };
        }

        public Task<IDictionary<string, IDictionary<string, List<int>>>> GetInvertedIndexAsync(CancellationToken cancel)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var result = (IDictionary<string, IDictionary<string, List<int>>>)
                Index.IndexData
                    .OrderBy(x => x.Key)
                    .ToDictionary(
                        x => x.Key,
                        x => (IDictionary<string, List<int>>) x.Value
                            .OrderBy(y => y.Key)
                            .ToDictionary(
                                y => GetTermText(x.Key, y.Key),
                                y => y.Value));
            return STT.Task.FromResult(result);
        }

        public Task<IDictionary<string, List<int>>> GetInvertedIndexAsync(string fieldName, CancellationToken cancel)
        {
            var raw = Index.IndexData
                .TryGetValue(fieldName, out var subIndex) ? subIndex : null;
            if (raw == null)
                return null;
            var result = raw
                .OrderBy(x=>x.Key)
                .ToDictionary(x => GetTermText(fieldName, x.Key), x => x.Value);
            return STT.Task.FromResult((IDictionary<string, List<int>>)result);
        }

        public IDictionary<string, string> GetIndexDocumentByVersionId(int versionId)
        {
            var key = InMemoryIndex.IntToString(versionId);
            if (!Index.IndexData["VersionId"].TryGetValue(key, out var docIds))
                return null;
            if (docIds.Count < 1)
                return null;
            return GetIndexDocumentByDocumentId(docIds[0]);
        }

        public IDictionary<string, string> GetIndexDocumentByDocumentId(int documentId)
        {
            var doc = new Dictionary<string, string>();
            foreach (var outerItem in Index.IndexData)
            {
                var fieldName = outerItem.Key;
                foreach (var innerItem in outerItem.Value)
                {
                    var term = GetTermText(fieldName, innerItem.Key);
                    if(innerItem.Value.Contains(documentId))
                    {
                        if (doc.TryGetValue(fieldName, out var fieldData))
                            doc[fieldName] = fieldData + "," + term;
                        else
                            doc[fieldName] = term;
                    }
                }
            }
            return doc;
        }
        private string GetTermText(string fieldName, string value)
        {
            if (value == null)
                return null;
            return NumberFields.Contains(fieldName) 
                ? value.Split('|').Last() 
                : value;
        }
    }
}

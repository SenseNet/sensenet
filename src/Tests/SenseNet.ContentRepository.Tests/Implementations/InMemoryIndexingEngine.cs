﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Tests.Implementations
{
    public class InMemoryIndexingEngine : IIndexingEngine
    {
        public InMemoryIndex Index { get; } = new InMemoryIndex();

        public bool Running { get; private set; }

        public bool WorksAsCentralizedIndex => false;

        public void Start(TextWriter consoleOut)
        {
            //UNDONE:|||||||||||| Remove call: IndexingActivityQueue.Startup
            IndexingActivityQueue.Startup(consoleOut);
            Running = true;
        }

        public void ShutDown()
        {
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

        public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> addition)
        {
            if (deletions != null)
                foreach (var term in deletions)
                    Index.Delete(term);

            if (updates != null)
                foreach (var update in updates)
                    Index.Update(update.UpdateTerm, (IndexDocument)update.Document);

            if (addition != null)
                foreach(var doc in addition)
                    Index.AddDocument(doc);
        }
    }
}

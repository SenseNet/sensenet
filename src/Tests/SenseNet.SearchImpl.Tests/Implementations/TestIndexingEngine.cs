using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class TestIndexingEngineFactory : IIndexingEngineFactory
    {
        //TODO: thread affinity to enable multithreaded ubit testing
        IIndexingEngine _instance = new TestIndexingEngine();

        public IIndexingEngine CreateIndexingEngine()
        {
            return _instance;
        }
    }

    internal class TestIndexingEngine : IIndexingEngine
    {
        public bool Running { get; private set; }
        public bool Paused { get; private set; }

        public void Pause()
        {
            Paused = true;
        }
        public void Continue()
        {
            Paused = false;
        }

        public void Start(TextWriter consoleOut)
        {
            Running = true;
        }

        public void WaitIfIndexingPaused()
        {
            // do nothing
        }

        public void ShutDown()
        {
            Running = false;
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void ActivityFinished()
        {
            // do nothing
        }

        public void Commit()
        {
            // do nothing
        }

        public IIndexingActivityStatus ReadActivityStatusFromIndex()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IIndexDocument> GetDocumentsByNodeId(int nodeId)
        {
            throw new NotImplementedException();
        }

        public void Actualize(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates)
        {
            throw new NotImplementedException();
        }

        public void Actualize(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> addition)
        {
            throw new NotImplementedException();
        }
    }
}

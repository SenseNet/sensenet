using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.SearchImpl.Tests.Implementations
{
    internal class InMemoryIndexingEngine : IIndexingEngine
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
            throw new NotImplementedException();
        }

        public void WaitIfIndexingPaused()
        {
            throw new NotImplementedException();
        }

        public void ShutDown()
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void ActivityFinished()
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public IIndexingActivityStatus ReadActivityStatusFromIndex()
        {
            return null;
        }

        public IEnumerable<IIndexDocument> GetDocumentsByNodeId(int nodeId)
        {
            throw new NotImplementedException();
        }
    }
}

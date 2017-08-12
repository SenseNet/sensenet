using System.Collections.Generic;
using System.IO;

namespace SenseNet.Search
{
    public interface IIndexingActivityStatus
    {
        int LastActivityId { get; set; }
        int[] Gaps { get; set; }
    }

    public interface IIndexingEngine // IIndexActualizator, IIndexingEngine
    {
        bool Running { get; }
        bool Paused { get; }
        void Pause();
        void Continue();
        void Start(TextWriter consoleOut);
        void WaitIfIndexingPaused();
        void ShutDown();
        void Restart();

        void ActivityFinished();
        void Commit();

        IIndexingActivityStatus ReadActivityStatusFromIndex();

        /// <summary>Only for tests.</summary>
        IEnumerable<IIndexDocument> GetDocumentsByNodeId(int nodeId);

        void Actualize(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates);
        void Actualize(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> addition);
    }

    public interface IIndexingEngineFactory
    {
        IIndexingEngine CreateIndexingEngine();
    }
}

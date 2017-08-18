using System.Collections.Generic;
using System.IO;

namespace SenseNet.Search
{
    public interface IIndexingActivityStatus
    {
        int LastActivityId { get; set; }
        int[] Gaps { get; set; }
    }
    public class IndexingActivityStatus : IIndexingActivityStatus //UNDONE: Refactor with CompletionStatus
    {
        public static IndexingActivityStatus Startup => new IndexingActivityStatus { Gaps = new int[0], LastActivityId = 0 };
        public int LastActivityId { get; set; }
        public int[] Gaps { get; set; }
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
        void Commit(int lastActivityId = 0);

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

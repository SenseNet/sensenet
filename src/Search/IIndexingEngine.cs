using System.Collections.Generic;
using System.IO;

namespace SenseNet.Search
{
    public interface IIndexingActivityStatus
    {
        int LastActivityId { get; set; }
        int[] Gaps { get; set; }
    }
    public class IndexingActivityStatus : IIndexingActivityStatus
    {
        public static IndexingActivityStatus Startup => new IndexingActivityStatus { Gaps = new int[0], LastActivityId = 0 };
        public int LastActivityId { get; set; }
        public int[] Gaps { get; set; }
    }

    public interface IIndexingEngine //UNDONE: Split or not: IIndexActualizator, IIndexingEngine
    {
        bool Running { get; }

        void Start(TextWriter consoleOut);

        void ShutDown();

        void ActivityFinished(); //UNDONE:!!!!! Remove if possible
        void Commit(int lastActivityId = 0); //UNDONE:!!!!! Remove if possible

        void ClearIndex();

        IIndexingActivityStatus ReadActivityStatusFromIndex();

        /// <summary>Only for tests.</summary>
        IEnumerable<IndexDocument> GetDocumentsByNodeId(int nodeId);

        void WriteIndex(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates); //UNDONE: rename to a better choice
        void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> addition); //UNDONE: rename to a better choice
    }
}

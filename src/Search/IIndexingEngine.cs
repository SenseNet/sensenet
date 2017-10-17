using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SenseNet.Search
{
    //public interface IIndexingActivityStatus
    //{
    //    int LastActivityId { get; set; }
    //    int[] Gaps { get; set; }
    //}
    public class IndexingActivityStatus //: IIndexingActivityStatus
    {
        public static IndexingActivityStatus Startup => new IndexingActivityStatus { Gaps = new int[0], LastActivityId = 0 };
        public int LastActivityId { get; set; }
        public int[] Gaps { get; set; }

        public override string ToString()
        {
            return $"{LastActivityId}({GapsToString(Gaps, 50, 10)})";
        }
        public static string GapsToString(int[] gaps, int maxCount, int growth)
        {
            if (gaps.Length < maxCount + growth)
                maxCount = gaps.Length;
            return (gaps.Length > maxCount)
                ? $"{string.Join(",", gaps.Take(maxCount))},... and {gaps.Length - maxCount} additional items"
                : string.Join(",", gaps);
        }
    }

    public interface IIndexingEngine
    {
        bool Running { get; }
        bool IndexIsCentralized { get; }

        void Start(TextWriter consoleOut);
        void ShutDown();
        void ClearIndex();

        IndexingActivityStatus ReadActivityStatusFromIndex();
        void WriteActivityStatusToIndex(IndexingActivityStatus state);

        void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> addition);
    }
}

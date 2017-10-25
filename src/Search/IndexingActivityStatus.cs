using System.Linq;

namespace SenseNet.Search
{
    public class IndexingActivityStatus
    {
        public static IndexingActivityStatus Startup => new IndexingActivityStatus { Gaps = new int[0], LastActivityId = 0 };
        public int LastActivityId { get; set; }
        public int[] Gaps { get; set; } = new int[0];

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
}

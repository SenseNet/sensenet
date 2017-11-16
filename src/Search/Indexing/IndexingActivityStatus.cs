using System.Linq;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Represents an indexing state
    /// </summary>
    public class IndexingActivityStatus
    {
        /// <summary>
        /// Shortcut of the empty index state.
        /// </summary>
        public static IndexingActivityStatus Startup => new IndexingActivityStatus { Gaps = new int[0], LastActivityId = 0 };

        /// <summary>
        /// Gets or sets the last written activity id.
        /// </summary>
        public int LastActivityId { get; set; }

        /// <summary>
        /// Gets or sets an array of the missing activity ids that are less than the LastActivityId.
        /// </summary>
        public int[] Gaps { get; set; } = new int[0];

        /// <summary>
        /// Returns with the string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return $"{LastActivityId}({GapsToString(Gaps, 50, 10)})";
        }

        /// <summary>
        /// Returns with the string representation of the given gaps.
        /// The string length can be limited with two parameter.
        /// If the gaps length is greater than {maxCount} + {growth},
        /// only the {maxCount} items will be converted. For example:
        /// "14, 16, 21,... and 20 additional items".
        /// </summary>
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

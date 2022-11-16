using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public static class SnTraceExtensions
    {
        public static string ToTrace(this string text, int maxLength = 100)
        {
            return text.Length < maxLength ? text : text.Substring(0, maxLength);
        }
        public static string ToTrace(this IEnumerable<int> items, int maxCount = 32)
        {
            var set = items.Take(maxCount + 1).Select(x => x.ToString()).ToArray();
            var text = string.Join(", ", set.Take(maxCount));
            return set.Length > 32 ? $"[{text}, ...]" : $"[{text}]";
        }
        public static string ToTrace(this IEnumerable<string> items, int maxCount = 10)
        {
            var set = items.Take(maxCount + 1).ToArray();
            var text = string.Join(", ", set.Take(maxCount));
            return set.Length > 10 ? $"[{text}, ...]" : $"[{text}]";
        }
    }
}

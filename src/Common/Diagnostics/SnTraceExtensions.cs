using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public static class SnTraceExtensions
    {
        public static SnTrace.Operation StartOperation(this SnTrace.SnTraceCategory traceCategory, Func<string> getMessage)
        {
            return traceCategory.StartOperation(traceCategory.Enabled ? getMessage() : string.Empty);
        }
        public static void Write(this SnTrace.SnTraceCategory traceCategory, Func<string> getMessage)
        {
            traceCategory.Write(traceCategory.Enabled ? getMessage() : string.Empty);
        }

        public static string ToTrace(this string text, int maxLength = 100) =>
            text.Length < maxLength ? text : text.Substring(0, maxLength);

        public static string ToTrace(this IEnumerable<int> items, int maxCount = 32) =>
            Format(items.Take(maxCount + 1).Select(x => x.ToString()).ToArray(), maxCount);

        public static string ToTrace(this IEnumerable<string> items, int maxCount = 10) =>
            Format(items.Take(maxCount + 1).ToArray(), maxCount);

        private static string Format(string[] set, int maxCount) =>
            $"[{string.Join(", ", set.Take(maxCount))}{(set.Length > maxCount ? ", ...]" : "]")}";
    }
}

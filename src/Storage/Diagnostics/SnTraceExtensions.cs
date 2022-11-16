using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public static class SnTraceExtensions
    {
        public static string ToTrace(this IDictionary<PropertyType, string> data)
        {
            // [Text1: Long text... (1234), Text2: 7 chars (7)]
            return string.Join(", ", data.Select(x =>
                $"{x.Key.Name}: {(x.Value.Length > 20 ? x.Value.Substring(0, 20) + "..." : x.Value)} ({x.Value.Length})"));
        }
        public static string ToTrace(this IDictionary<PropertyType, List<int>> data, int maxCount = 8)
        {
            // [Refs1: [], Refs2: []]
            return string.Join(", ", data.Select(x =>
                $"{x.Key.Name}: {x.Value.ToTrace(maxCount)}"));
        }
    }
}

using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public class SnTraceTools
    {
        public static string Truncate(string text, int maxLength = 100)
        {
            return text.Length < maxLength ? text : text.Substring(0, maxLength);
        }

        public static string ConvertToString(IEnumerable<int> items)
        {
            var set = items.Take(33).Select(x=>x.ToString()).ToArray();
            var text = string.Join(", ", set.Take(32));
            return set.Length > 32 ? $"[{text}, ...]" : $"[{text}]";
        }
        public static string ConvertToString(IEnumerable<string> items)
        {
            var set = items.Take(11).ToArray();
            var text = string.Join(", ", set.Take(10));
            return set.Length > 10 ? $"[{text}, ...]" : $"[{text}]";
        }
    }
}

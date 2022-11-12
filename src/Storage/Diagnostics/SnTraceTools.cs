// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public class SnTraceTools
    {
        public static string Truncate(string text, int maxLength = 100)
        {
            return text.Length < maxLength ? text : text.Substring(0, maxLength);
        }
    }
}

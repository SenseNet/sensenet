using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;

namespace SenseNet.Search
{
    internal class TextSplitter
    {
        public static readonly char[] WhitespaceChars = " \t\r\n".ToCharArray();
        public static readonly char[] StandardSplitChars = "_ \t\r\n".ToCharArray();

        internal static string[] SplitText(string fieldName, string text)
        {
            return SplitText(fieldName, text, SenseNet.ContentRepository.Storage.StorageContext.Search.SearchEngine.GetAnalyzers());
        }
        internal static string[] SplitText(string fieldName, string text, IDictionary<string, Type> analyzers)
        {
            if (String.IsNullOrEmpty(fieldName))
                fieldName = LucObject.FieldName.AllText;
            Type analyzerType;
            if (!analyzers.TryGetValue(fieldName, out analyzerType) && fieldName != LucObject.FieldName.AllText)
                analyzerType = typeof(KeywordAnalyzer);
            var needToSplit = analyzerType != typeof(KeywordAnalyzer);

            string[] words;
            if (analyzerType == typeof(WhitespaceAnalyzer))
                words = text.ToLower().Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries);
            else if (analyzerType != typeof(KeywordAnalyzer))
                words = SplitByNonAlphanum(text.ToLower());
            else
                words = new string[] { text.ToLower() };

            return words;
        }
        private static string[] SplitByNonAlphanum(string text)
        {
            var words = new List<string>();
            var word = new StringBuilder(text.Length);
            var p = -1;
            while (++p < text.Length)
            {
                var c = text[p];
                if (Char.IsLetterOrDigit(c))
                {
                    word.Append(c);
                }
                else
                {
                    if (word.Length > 0)
                        words.Add(word.ToString());
                    if (word.Length > 0)
                        word.Length = 0;
                }
            }
            if (word.Length > 0)
                words.Add(word.ToString());
            return words.ToArray();
        }
    }
}

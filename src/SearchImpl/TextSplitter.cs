using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Search
{
    internal class TextSplitter
    {
        public static readonly char[] WhitespaceChars = " \t\r\n".ToCharArray();
        public static readonly char[] StandardSplitChars = "_ \t\r\n".ToCharArray();

        internal static string[] SplitText(string fieldName, string text)
        {
            return SplitText(fieldName, text, ContentRepository.Storage.StorageContext.Search.SearchEngine.GetAnalyzers());
        }
        internal static string[] SplitText(string fieldName, string text, IDictionary<string, IndexFieldAnalyzer> analyzers)
        {
            if (String.IsNullOrEmpty(fieldName))
                fieldName = IndexFieldName.AllText;
            IndexFieldAnalyzer analyzerType;
            if (!analyzers.TryGetValue(fieldName, out analyzerType) && fieldName != IndexFieldName.AllText)
                analyzerType = IndexFieldAnalyzer.Keyword;

            string[] words;
            if (analyzerType == IndexFieldAnalyzer.Whitespace)
                words = text.ToLower().Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries);
            else if (analyzerType != IndexFieldAnalyzer.Keyword)
                words = SplitByNonAlphanum(text.ToLower());
            else
                words = new[] {text.ToLower()};

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
                if (char.IsLetterOrDigit(c))
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

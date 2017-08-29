using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Store;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Lucene29
{
    public static class Lucene29IndexManager //UNDONE:!!!!!! Merge functionality into the indexing engine (even as static)
    {
        public static Analyzer GetAnalyzer()
        {
            var defaultAnalyzer = new KeywordAnalyzer();
            var analyzer = new Indexing.SnPerFieldAnalyzerWrapper(defaultAnalyzer);

            return analyzer;
        }
    }

}

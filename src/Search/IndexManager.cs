using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Store;

namespace SenseNet.Search
{
    internal class IndexManager
    {
        static IndexManager()
        {
            IndexWriter.SetDefaultWriteLockTimeout(20 * 60 * 1000); // 20 minutes
        }

        internal static IndexWriter GetIndexWriter(bool createNew)
        {
            Directory dir = FSDirectory.Open(new System.IO.DirectoryInfo(SenseNet.ContentRepository.Storage.IndexDirectory.CurrentOrDefaultDirectory));
            return new IndexWriter(dir, GetAnalyzer(), createNew, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        internal static Analyzer GetAnalyzer()
        {
            var defaultAnalyzer = new KeywordAnalyzer();
            var analyzer = new Indexing.SnPerFieldAnalyzerWrapper(defaultAnalyzer);

            return analyzer;
        }
    }

}

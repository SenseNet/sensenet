using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;

namespace SenseNet.Search.Indexing
{
    public class IndexReaderFrame : IDisposable
    {
        private IndexReader _reader;
        public IndexReader IndexReader { get { return _reader; } }

        public IndexReaderFrame(IndexReader reader)
        {
            _reader = reader;
            reader.IncRef();
        }

        public void Dispose()
        {
            _reader.DecRef();
        }
    }
}

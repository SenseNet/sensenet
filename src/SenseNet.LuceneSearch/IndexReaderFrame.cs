using System;
using Lucene.Net.Index;

namespace SenseNet.Search.Lucene29
{
    public class IndexReaderFrame : IDisposable
    {
        public IndexReader IndexReader { get; }

        public IndexReaderFrame(IndexReader reader)
        {
            IndexReader = reader;
            reader.IncRef();
        }

        public void Dispose()
        {
            IndexReader.DecRef();
        }
    }
}

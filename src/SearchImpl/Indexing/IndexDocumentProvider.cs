using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Search.Indexing
{
    public class IndexDocumentProvider : IIndexDocumentProvider
    {
        public object GetIndexDocumentInfo(Node node, bool skipBinaries, bool isNew, out bool hasBinary)
        {
            var x = IndexDocumentInfo.Create(node, skipBinaries, isNew);
            hasBinary = x.HasBinaryField;
            return x;
        }
        public object CompleteIndexDocumentInfo(Node node, object baseDocumentInfo)
        {
            return ((IndexDocumentInfo)baseDocumentInfo).Complete(node);
        }


        public IndexDocument GetIndexDocument(Node node, bool skipBinaries, bool isNew, out bool hasBinary)
        {
            var indxDoc = new IndexDocument();
            hasBinary = false;

            throw new NotImplementedException();

            return indxDoc;
        }
    }
}

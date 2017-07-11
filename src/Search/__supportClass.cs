using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.Search
{
    internal class __supportClass
    {
        internal class IndexDocumentInfo
        {
            public List<IIndexFieldInfo> Fields
            {
                get { throw new NotImplementedException(); }
            }

            public static Document CreateDocument(IndexDocumentInfo info, IndexDocumentData docData)
            {
                throw new NotImplementedException();
            }

            public static Document GetDocument(IndexDocumentData docData)
            {
                throw new NotImplementedException();
            }

            public static Document GetDocument(int versionId)
            {
                throw new NotImplementedException();
            }

            public static IEnumerable<Document> GetDocuments(IEnumerable<int> versionIdSet)
            {
                throw new NotImplementedException();
            }
        }

        internal class LucQuery
        {
            public static LucQuery Create(NodeQuery nodeQuery)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<LucObject> Execute()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<LucObject> Execute(bool allVersions)
            {
                throw new NotImplementedException();
            }

            public static LucQuery Parse(string lucQuery)
            {
                throw new NotImplementedException();
            }

            public Query Query { get; set; }
        }
        internal class LucObject
        {
            public int NodeId { get; set; }
        }
    }
}

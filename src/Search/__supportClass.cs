using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Search
{
    internal class __supportClass
    {
        internal class ContentTypeManager
        {
            internal static PerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
            {
                throw new NotImplementedException();
            }
        }

        internal class PerFieldIndexingInfo
        {
            public string Analyzer { get; set; }
            public FieldIndexHandler IndexFieldHandler { get; set; }

        }

        internal class FieldIndexHandler
        {
            public virtual string GetDefaultAnalyzerName()
            {
                throw new NotImplementedException();
            }
        }


        public enum FieldInfoType
        {
            StringField,
            IntField,
            LongField,
            SingleField,
            DoubleField
        }

        internal class IndexDocumentInfo
        {
            public List<IndexFieldInfo> Fields
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

        internal class IndexFieldInfo
        {
            public string Name;
            public string Value;

            public IndexFieldInfo(string allText, string textExtract, FieldInfoType stringField, Field.Store nO1,
                Field.Index aNALYZED, Field.TermVector nO2)
            {
                throw new NotImplementedException();
            }
        }

        public class BooleanIndexHandler : FieldIndexHandler //, IIndexValueConverter<bool>, IIndexValueConverter
        {
            public static readonly string YES = "yes";
            public static readonly string NO = "no";
        }

        public class ContentType
        {
            public string Name;
            public bool IndexingEnabled;
        }

        public class AllContentTypes : IEnumerable<ContentType>
        {
            public int Count()
            {
                throw new NotImplementedException();
            }

            public bool Contains(ContentType item)
            {
                return true;
            }

            public IEnumerator<ContentType> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal class RepositoryInstance
        {
            public static bool RestoreIndexOnStartup()
            {
                throw new NotImplementedException();
            }

            public static bool WaitForWriterLockFileIsReleased(string indexDirectoryBackupPath)
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
        }

        internal class LucObject
        {
            public int NodeId { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Storage.Data
{
    [Serializable]
    public class IndexDocumentData
    {
        [NonSerialized]
        private IndexDocument _indexDocument;
        public IndexDocument IndexDocument
        {
            get
            {
                if (_indexDocument == null)
                    _indexDocument = IndexDocument.Deserialize(_indexDocumentBytes);
                return _indexDocument;
            }
        }

        private byte[] _indexDocumentBytes;
        public byte[] IndexDocumentInfoBytes //UNDONE:!!!!!!!!! Rename to SerializedIndexDocument
        {
            get
            {
                if (_indexDocumentBytes == null)
                {
                    using (var docStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        formatter.Serialize(docStream, _indexDocument);
                        docStream.Flush();
                        IndexDocumentInfoSize = docStream.Length;
                        _indexDocumentBytes = docStream.GetBuffer();
                    }
                }
                return _indexDocumentBytes;
            }
        }
        public long? IndexDocumentInfoSize { get; set; }

        public int NodeTypeId { get; set; }
        public int VersionId { get; set; }
        public int NodeId { get; set; }
        public string Path { get; set; }
        public int ParentId { get; set; }
        public bool IsSystem { get; set; }
        public bool IsLastDraft { get; set; }
        public bool IsLastPublic { get; set; }
        public long NodeTimestamp { get; set; }
        public long VersionTimestamp { get; set; }

        public IndexDocumentData(IndexDocument indexDocument, byte[] indexDocumentBytes)
        {
            _indexDocument = indexDocument;
            _indexDocumentBytes = indexDocumentBytes;
        }

        public void IndexDocumentChanged()
        {
            _indexDocumentBytes = null;
        }
    }
}

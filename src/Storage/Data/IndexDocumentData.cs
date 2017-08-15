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
        private object _indexDocumentInfo;
        public object IndexDocumentInfo //UNDONE:!!!!!!!!! Rename to IndexDocument
        {
            get
            {
                if (_indexDocumentInfo == null)
                    _indexDocumentInfo = StorageContext.Search.SearchEngine.DeserializeIndexDocumentInfo(IndexDocumentInfoBytes);
                return _indexDocumentInfo;
            }
        }

        [NonSerialized]
        private IndexDocument _indexDocument;
        public IndexDocument IndexDocument
        {
            get
            {
                if (_indexDocument == null)
                    _indexDocument = SenseNet.Search.IndexDocument.Deserialize(_indexDocumentInfoBytes);
                return _indexDocument;
            }
        }

        private byte[] _indexDocumentInfoBytes;
        public byte[] IndexDocumentInfoBytes //UNDONE:!!!!!!!!! Rename to SerializedIndexDocument
        {
            get
            {
                if (_indexDocumentInfoBytes == null)
                {
                    using (var docStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        formatter.Serialize(docStream, _indexDocumentInfo);
                        docStream.Flush();
                        IndexDocumentInfoSize = docStream.Length;
                        _indexDocumentInfoBytes = docStream.GetBuffer();
                    }
                }
                return _indexDocumentInfoBytes;
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

        public IndexDocumentData(object indexDocumentInfo, byte[] indexDocumentInfoBytes)
        {
            _indexDocumentInfo = indexDocumentInfo;
            _indexDocumentInfoBytes = indexDocumentInfoBytes;
        }
    }
}

using System;
using System.IO;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository
{
    public class BinaryTypeResolver
    {
        private string _fileNameExtension;
        private string _contentType;
        private NodeType _nodeType;

        public BinaryTypeResolver()
        {
        }

        public bool ParseBinary(BinaryData binaryData)
        {
            if(binaryData == null)
                throw new ArgumentNullException("binaryData");

            // TODO: Resolve correct File subtype by the SenseNet.ContentRepository.Storage.MimeTable
            _nodeType = ActiveSchema.NodeTypes[typeof(File).Name];
            if(_nodeType == null)
            {
                // Unknown type
                _fileNameExtension = null;
                _contentType = null;
                return false;
            }
            else
            {
                // Fix extension and/or contenttype values by config matching
                _fileNameExtension = binaryData.FileName.Extension;
                _contentType = binaryData.ContentType;
                return true;
            }
        }

        public string FileNameExtension
        {
            get { return _fileNameExtension; }
        }
        public string ContentType
        {
            get { return _contentType; }
        }
        public NodeType NodeType
        {
            get { return _nodeType; }
        }


    }
}
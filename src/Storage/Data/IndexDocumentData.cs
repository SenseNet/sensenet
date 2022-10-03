﻿using System;
using Newtonsoft.Json;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Represents a class that encapsulates the persistent mutation of the indexing document.
    /// </summary>
    [Serializable]
    public class IndexDocumentData
    {
        [NonSerialized]
        private IndexDocument _indexDocument;

        /// <summary>
        /// Gets the index document. If this instance is initialized with the
        /// serialized version, the deserialization will be executed.
        /// </summary>
        public IndexDocument IndexDocument
        {
            get
            {
                if (_indexDocument == null)
                    _indexDocument = IndexDocument.Deserialize(_serializedIndexDocument);
                return _indexDocument;
            }
        }

        private string _serializedIndexDocument;
        /// <summary>
        /// Gets the serialized index document. If this instance is initialized with an
        /// IndexDocument instance or invalidated, the serialization will be executed.
        /// </summary>
        [JsonIgnore]
        public string SerializedIndexDocument
        {
            get
            {
                if (_serializedIndexDocument == null)
                    _serializedIndexDocument = _indexDocument.Serialize();
                return _serializedIndexDocument;
            }
        }

        /// <summary>
        /// Gets the size of the serialized version if there is, otherwise null. 
        /// </summary>
        public long? IndexDocumentSize => SerializedIndexDocument?.Length;

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

        /// <summary>
        /// Initialites a new instance of IndexDocumentData with one of the possible parameters.
        /// Both parameters cannot be null at one time.
        /// </summary>
        /// <param name="indexDocument">The index document.</param>
        /// <param name="serializedIndexDocument">Serialized data from the database.</param>
        public IndexDocumentData(IndexDocument indexDocument, string serializedIndexDocument)
        {
            _indexDocument = indexDocument;
            _serializedIndexDocument = serializedIndexDocument;
        }

        /// <summary>
        /// Invalidates the serialized document when any data changed.
        /// </summary>
        public void IndexDocumentChanged()
        {
            _serializedIndexDocument = null;
        }
    }
}

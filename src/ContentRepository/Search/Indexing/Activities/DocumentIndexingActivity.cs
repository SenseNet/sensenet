using System;
using System.Threading;
using STT=System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal class VersioningInfo
    {
        public int LastPublicVersionId;
        public int LastDraftVersionId;

        public int[] Delete;
        public int[] Reindex;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };
        internal static VersioningInfo Deserialize(string value)
        {
            if (value == null)
                return new VersioningInfo();

            var result = (VersioningInfo)JsonConvert.DeserializeObject(value, typeof(VersioningInfo), SerializerSettings);
            return result;
        }

        internal static string Serialize(VersioningInfo value)
        {
            if (value == null)
                return null;
            if (
                (value.Delete == null || value.Delete.Length == 0) &&
                (value.Delete == null || value.Reindex.Length == 0) &&
                value.LastDraftVersionId == 0
                && value.LastPublicVersionId == 0)
                return null;

            var result = JsonConvert.SerializeObject(value);
            return result;
        }
    }

    [Serializable]
    internal abstract class DocumentIndexingActivity : IndexingActivityBase
    {
        private bool _documentIsCreated;

        private IndexDocument _document;
        public IndexDocument Document
        {
            get
            {
                if (!_documentIsCreated)
                {
                    _document = CreateDocument();
                    _documentIsCreated = true;
                }
                return _document;
            }
        }

        public VersioningInfo Versioning { get; set; }

        public virtual IndexDocument CreateDocument()
        {
            using (var op = SnTrace.Index.StartOperation("LM: DocumentIndexingActivity.CreateDocument (VersionId:{0}) A{1}", VersionId, Id))
            {
                IndexDocument doc;
                if (IndexDocumentData != null)
                {
                    // create document from indexdocumentdata if it has been supplied (eg via MSMQ if it was small enough to send it over)
                    doc = IndexManager.CompleteIndexDocument(IndexDocumentData);

                    if (doc == null)
                        SnTrace.Index.Write("LM: DocumentIndexingActivity.CreateDocument (VersionId:{0}): Document is NULL from QUEUE", VersionId);
                }
                else
                {
                    // create document via loading it from db (eg when indexdocumentdata was too large to send over MSMQ)
                    doc = IndexManager.LoadIndexDocumentByVersionId(VersionId);

                    if (doc == null)
                        SnTrace.Index.Write("LM: DocumentIndexingActivity.CreateDocument (VersionId:{0}): Document is NULL from DB.", VersionId);
                }
                op.Successful = true;
                return doc;
            }
        }

        public void SetDocument(IndexDocument document)
        {
            _document = document;
            _documentIsCreated = true;
        }

        public override string ToString()
        {
            return $"{GetType().Name}: [{NodeId}/{VersionId}], {Path}";
        }

        public override async STT.Task DistributeAsync(CancellationToken cancellationToken)
        {
            // check doc size before distributing
            var sendDocOver = IndexDocumentData?.IndexDocumentSize != null &&
                              IndexDocumentData.IndexDocumentSize.Value < Messaging.MsmqIndexDocumentSizeLimit;

            if (sendDocOver)
            {
                // document is small enough to send over in a message
                await base.DistributeAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // document is too large, send activity without the document
                SnTrace.Index.Write("Activity is truncated. Id:{0}, Type:{1}", Id, ActivityType);
                var docData = IndexDocumentData;
                IndexDocumentData = null;

                await base.DistributeAsync(cancellationToken).ConfigureAwait(false);

                // restore index document after activity is sent
                IndexDocumentData = docData;
            }
        }

        protected override string GetExtension()
        {
            return VersioningInfo.Serialize(Versioning);
        }
        protected override void SetExtension(string value)
        {
            Versioning = VersioningInfo.Deserialize(value);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SnD = SenseNet.Diagnostics;
using SnS = SenseNet.ContentRepository.Storage;
using SnSS = SenseNet.ContentRepository.Storage.Schema;
using System.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Tools;
using Field = Lucene.Net.Documents.Field;

namespace SenseNet.Search.Indexing
{
    [Serializable]
    public class IndexDocumentInfo
    {
        [Serializable]
        public class NotIndexedIndexDocumentInfo : IndexDocumentInfo { }

        [NonSerialized]
        private static IPerFieldIndexingInfo __nameFieldIndexingInfo;
        [NonSerialized]
        private static IPerFieldIndexingInfo __pathFieldIndexingInfo;
        [NonSerialized]
        private static IPerFieldIndexingInfo __inTreeFieldIndexingInfo;
        [NonSerialized]
        private static IPerFieldIndexingInfo __inFolderFieldIndexingInfo;

        internal static IPerFieldIndexingInfo NameFieldIndexingInfo
        {
            get
            {
                if (__nameFieldIndexingInfo == null)
                    __nameFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.Name);
                return __nameFieldIndexingInfo;
            }
        }
        internal static IPerFieldIndexingInfo PathFieldIndexingInfo
        {
            get
            {
                if (__pathFieldIndexingInfo == null)
                    __pathFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.Path);
                return __pathFieldIndexingInfo;
            }
        }
        internal static IPerFieldIndexingInfo InTreeFieldIndexingInfo
        {
            get
            {
                if (__inTreeFieldIndexingInfo == null)
                    __inTreeFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.InTree);
                return __inTreeFieldIndexingInfo;
            }
        }
        internal static IPerFieldIndexingInfo InFolderFieldIndexingInfo
        {
            get
            {
                if (__inFolderFieldIndexingInfo == null)
                    __inFolderFieldIndexingInfo = StorageContext.Search.ContentRepository.GetPerFieldIndexingInfo(IndexFieldName.InFolder);
                return __inFolderFieldIndexingInfo;
            }
        }

        [NonSerialized]
        private static readonly IEnumerable<Document> EmptyDocuments = new Document[0];

        private List<IIndexFieldInfo> fields = new List<IIndexFieldInfo>();
        public List<IIndexFieldInfo> Fields
        {
            get { return fields; }
        }

        private bool _hasCustomField;
        public bool HasCustomField
        {
            get { return _hasCustomField; }
        }
        public bool HasBinaryField { get; private set; }

        [NonSerialized]
        internal static readonly IndexDocumentInfo NotIndexedDocument = new NotIndexedIndexDocumentInfo();

        public void AddField(string name, string value, IndexStoringMode store, IndexingMode index)
        {
            AddField(name, value, store, index, IndexTermVector.No);
        }
        public void AddField(string name, string value, IndexStoringMode store, IndexingMode index, IndexTermVector termVector)
        {
            fields.Add(new IndexFieldInfo(name, value, FieldInfoType.StringField, store, index, termVector));
        }
        public void AddField(string name, int value, IndexStoringMode store, bool isIndexed)
        {
            fields.Add(new IndexFieldInfo(name, value.ToString(CultureInfo.InvariantCulture), FieldInfoType.IntField, store, isIndexed ? IndexingMode.AnalyzedNoNorms : IndexingMode.No, IndexTermVector.No));
        }
        public void AddField(string name, long value, IndexStoringMode store, bool isIndexed)
        {
            fields.Add(new IndexFieldInfo(name, value.ToString(CultureInfo.InvariantCulture), FieldInfoType.LongField, store, isIndexed ? IndexingMode.AnalyzedNoNorms : IndexingMode.No, IndexTermVector.No));
        }
        public void AddField(string name, double value, IndexStoringMode store, bool isIndexed)
        {
            fields.Add(new IndexFieldInfo(name, value.ToString(CultureInfo.InvariantCulture), FieldInfoType.DoubleField, store, isIndexed ? IndexingMode.AnalyzedNoNorms : IndexingMode.No, IndexTermVector.No));
        }
        public void AddField(IIndexFieldInfo fieldInfo)
        {
            fields.Add(fieldInfo);
        }

        [NonSerialized]
        private static List<string> PostponedFields = new List<string>(new string[] {
            IndexFieldName.Name, IndexFieldName.Path, IndexFieldName.InTree, IndexFieldName.InFolder, IndexFieldName.Depth, IndexFieldName.ParentId,
            IndexFieldName.IsSystem
        });
        [NonSerialized]
        private static List<string> ForbiddenFields = new List<string>(new string[] { "Password", "PasswordHash" });
        private static List<string> SkippedMultistepFields = new List<string>(new[] { "Size" });

        private static bool Indexable(Node node)
        {
            return StorageContext.Search.ContentRepository.IsContentTypeIndexed(node.NodeType.Name);
        }

        public static IndexDocumentInfo Create(Node node, bool skipBinaries, bool isNew)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (!Indexable(node))
                return NotIndexedDocument;

            var textEtract = new StringBuilder();
            var doc = new IndexDocumentInfo();

            doc._hasCustomField = node is IHasCustomIndexField;

            var ixnode = node as IIndexableDocument;
            var faultedFieldNames = new List<string>();

            if (ixnode == null)
            {
                doc.AddField(IndexFieldName.NodeId, node.Id, IndexStoringMode.Yes, true);
                doc.AddField(IndexFieldName.VersionId, node.VersionId, IndexStoringMode.Yes, true);
                doc.AddField(IndexFieldName.Version, node.Version.ToString().ToLowerInvariant(), IndexStoringMode.Yes, IndexingMode.Analyzed);
                doc.AddField(IndexFieldName.OwnerId, node.OwnerId, IndexStoringMode.Yes, true);
                doc.AddField(IndexFieldName.CreatedById, node.CreatedById, IndexStoringMode.Yes, true);
                doc.AddField(IndexFieldName.ModifiedById, node.ModifiedById, IndexStoringMode.Yes, true);
            }
            else
            {
                var fieldNames = new List<string>();
                foreach (var field in ixnode.GetIndexableFields())
                {
                    if (ForbiddenFields.Contains(field.Name))
                        continue;
                    if (PostponedFields.Contains(field.Name))
                        continue;
                    if (node.SavingState != ContentSavingState.Finalized && (field.IsBinaryField || SkippedMultistepFields.Contains(field.Name)))
                        continue;
                    if (skipBinaries && (field.IsBinaryField))
                    {
                        if (!StorageContext.Search.ContentRepository.TextExtractingWillBePotentiallySlow(field))
                        {
                            doc.HasBinaryField = true;
                            continue;
                        }
                    }

                    IEnumerable<IIndexFieldInfo> lucFields = null;
                    string extract = null;
                    try
                    {
                        lucFields = field.GetIndexFieldInfos(out extract);
                    }
                    catch (Exception)
                    {
                        faultedFieldNames.Add(field.Name);
                    }

                    if (!String.IsNullOrEmpty(extract)) // do not add extra line if extract is empty
                    {
                        try
                        {
                            textEtract.AppendLine(extract);
                        }
                        catch (OutOfMemoryException)
                        {
                            SnLog.WriteWarning("Out of memory error during indexing.",
                                EventId.Indexing,
                                properties: new Dictionary<string, object>
                                    {
                                        { "Path", node.Path },
                                        { "Field", field.Name }
                                    });
                        }
                    }

                    if (lucFields != null)
                    {
                        foreach (var lucField in lucFields)
                        {
                            fieldNames.Add(lucField.Name);
                            doc.AddField(lucField);
                        }
                    }
                }
            }

            var isInherited = true;
            if (!isNew)
                isInherited = node.IsInherited;
            doc.AddField(IndexFieldName.IsInherited, isInherited ? StorageContext.Search.Yes : StorageContext.Search.No, IndexStoringMode.Yes, IndexingMode.NotAnalyzed);
            doc.AddField(IndexFieldName.IsMajor, node.Version.IsMajor ? StorageContext.Search.Yes : StorageContext.Search.No, IndexStoringMode.Yes, IndexingMode.NotAnalyzed);
            doc.AddField(IndexFieldName.IsPublic, node.Version.Status == VersionStatus.Approved ? StorageContext.Search.Yes : StorageContext.Search.No, IndexStoringMode.Yes, IndexingMode.NotAnalyzed);
            doc.AddField(IndexFieldName.AllText, textEtract.ToString(), IndexStoringMode.No, IndexingMode.Analyzed);

            if (faultedFieldNames.Any())
            {
                doc.AddField(IndexFieldName.IsFaulted, StorageContext.Search.Yes , IndexStoringMode.Yes, IndexingMode.NotAnalyzed);
                foreach (var faultedFieldName in faultedFieldNames)
                    doc.AddField(IndexFieldName.FaultedFieldName, faultedFieldName, IndexStoringMode.Yes, IndexingMode.NotAnalyzed);
            }

            ValidateDocumentInfo(doc);

            return doc;
        }
        public IndexDocumentInfo Complete(Node node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var allTextFieldIndex = -1;
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Name == IndexFieldName.AllText)
                {
                    allTextFieldIndex = i;
                    break;
                }
            }
            var allTextField = this.fields[allTextFieldIndex];
            var textEtract = new StringBuilder(allTextField.Value);
            var faultedFieldNames = new List<string>();

            var ixnode = node as IIndexableDocument;

            if (ixnode != null)
            {
                var fieldNames = new List<string>();
                foreach (var field in ixnode.GetIndexableFields())
                {
                    if (ForbiddenFields.Contains(field.Name))
                        continue;
                    if (PostponedFields.Contains(field.Name))
                        continue;
                    if (node.SavingState != ContentSavingState.Finalized && (field.IsBinaryField || SkippedMultistepFields.Contains(field.Name)))
                        continue;
                    if (!field.IsBinaryField)
                        continue;
                    if(!StorageContext.Search.ContentRepository.TextExtractingWillBePotentiallySlow(field))
                        continue;

                    IEnumerable<IIndexFieldInfo> lucFields = null;
                    string extract = null;
                    try
                    {
                        lucFields = field.GetIndexFieldInfos(out extract);
                    }
                    catch (Exception)
                    {
                        faultedFieldNames.Add(field.Name);
                    }

                    if (!String.IsNullOrEmpty(extract)) // do not add extra line if extract is empty
                    {
                        try
                        {
                            textEtract.AppendLine(extract);
                        }
                        catch (OutOfMemoryException)
                        {
                            SnLog.WriteWarning("Out of memory error during indexing.",
                                EventId.Indexing,
                                properties: new Dictionary<string, object>
                                {
                                    { "Path", node.Path },
                                    { "Field", field.Name }
                                });
                        }
                    }

                    if (lucFields != null)
                    {
                        foreach (var lucField in lucFields)
                        {
                            fieldNames.Add(lucField.Name);
                            this.AddField(lucField);
                        }
                    }
                }
            }

            fields[allTextFieldIndex] = new IndexFieldInfo(IndexFieldName.AllText, textEtract.ToString(), FieldInfoType.StringField, IndexStoringMode.No, IndexingMode.Analyzed, IndexTermVector.No);

            if (faultedFieldNames.Any())
            {
                if (!this.fields.Any(f => f.Name == IndexFieldName.IsFaulted))
                    this.AddField(IndexFieldName.IsFaulted, StorageContext.Search.Yes, IndexStoringMode.Yes, IndexingMode.NotAnalyzed);
                foreach (var faultedFieldName in faultedFieldNames)
                    this.AddField(IndexFieldName.FaultedFieldName, faultedFieldName, IndexStoringMode.Yes, IndexingMode.Analyzed);
            }

            return this;
        }

        internal static Document GetDocument(int versionId)
        {
            using (var op = SnTrace.Index.StartOperation("Load IndexDocumentInfo. VersionId:{0}", versionId))
            {
                var docData = StorageContext.Search.LoadIndexDocumentByVersionId(versionId);
                if (docData == null)
                    return null;
                var doc = GetDocument(docData);
                op.Successful = true;
                return doc;
            }
        }
        internal static IEnumerable<Document> GetDocuments(IEnumerable<int> versionIdSet)
        {
            var vset = versionIdSet.ToArray();
            if (vset.Length == 0)
                return EmptyDocuments;
            var docData = StorageContext.Search.LoadIndexDocumentByVersionId(versionIdSet);
            var result = docData.Select(d => GetDocument(d)).Where(x => x != null).ToArray(); // null means: indexing disabled
            return result;
        }

        internal static Document GetDocument(IndexDocumentData docData)
        {
            var buffer = docData.IndexDocumentInfoBytes;

            var docStream = new System.IO.MemoryStream(buffer);
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var info = (IndexDocumentInfo)formatter.Deserialize(docStream);

            if (info is NotIndexedIndexDocumentInfo)
                return null;

            return CreateDocument(info, docData);
        }
        public static Document CreateDocument(Node node, bool isNew) // caller: tests
        {
            var info = Create(node, false, isNew);
            var data = new IndexDocumentData(null, null)
            {
                Path = node.Path,
                ParentId = node.ParentId,
                IsSystem = node.IsSystem,
                IsLastPublic = node.IsLastPublicVersion,
                IsLastDraft = node.IsLatestVersion
            };
            return CreateDocument(info, data);
        }
        internal static Document CreateDocument(IndexDocumentInfo info, IndexDocumentData docData)
        {
            if (info == null)
                return null;
            if (info is NotIndexedIndexDocumentInfo)
                return null;

            var doc = new Document();
            foreach (var fieldInfo in info.fields)
                if (fieldInfo.Name != "Password" && fieldInfo.Name != "PasswordHash")
                    doc.Add(CreateField(fieldInfo));

            var path = docData.Path.ToLowerInvariant();

            doc.Add(CreateStringField(IndexFieldName.Name, RepositoryPath.GetFileName(path), NameFieldIndexingInfo));
            doc.Add(CreateStringField(IndexFieldName.Path, path, PathFieldIndexingInfo));

            var nf = new NumericField(IndexFieldName.Depth, Field.Store.YES, true);
            nf.SetIntValue(Node.GetDepth(docData.Path));
            doc.Add(nf);


            var fields = CreateInTreeFields(IndexFieldName.InTree, docData.Path);
            foreach (var field in fields)
                doc.Add(field);

            doc.Add(CreateInFolderField(IndexFieldName.InFolder, path));

            nf = new NumericField(IndexFieldName.ParentId, Field.Store.NO, true);
            nf.SetIntValue(docData.ParentId);
            doc.Add(nf);

            doc.Add(new Field(IndexFieldName.IsSystem, docData.IsSystem ? StorageContext.Search.Yes : StorageContext.Search.No, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

            // flags
            doc.Add(new Field(IndexFieldName.IsLastPublic, docData.IsLastPublic ? StorageContext.Search.Yes : StorageContext.Search.No, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
            doc.Add(new Field(IndexFieldName.IsLastDraft, docData.IsLastDraft ? StorageContext.Search.Yes : StorageContext.Search.No, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

            // timestamps
            nf = new NumericField(IndexFieldName.NodeTimestamp, Field.Store.YES, true);
            nf.SetLongValue(docData.NodeTimestamp);
            doc.Add(nf);
            nf = new NumericField(IndexFieldName.VersionTimestamp, Field.Store.YES, true);
            nf.SetLongValue(docData.VersionTimestamp);
            doc.Add(nf);

            // custom fields
            if (info.HasCustomField)
            {
                var customFields = CustomIndexFieldManager.GetFields(info, docData);
                if (customFields != null)
                    foreach (var field in customFields)
                        doc.Add(field);
            }

            return doc;
        }
        private static AbstractField CreateField(IIndexFieldInfo fieldInfo)
        {
            var store = EnumConverter.ToLuceneIndexStoringMode(fieldInfo.Store);
            NumericField nf;
            switch (fieldInfo.Type)
            {
                case FieldInfoType.StringField:
                    return new Field(fieldInfo.Name, fieldInfo.Value, store, EnumConverter.ToLuceneIndexingMode(fieldInfo.Index), EnumConverter.ToLuceneIndexTermVector(fieldInfo.TermVector));
                case FieldInfoType.IntField:
                    nf = new NumericField(fieldInfo.Name, store, fieldInfo.Index != IndexingMode.No);
                    nf.SetIntValue(Int32.Parse(fieldInfo.Value, CultureInfo.InvariantCulture));
                    return nf;
                case FieldInfoType.LongField:
                    nf = new NumericField(fieldInfo.Name, 8, store, fieldInfo.Index != IndexingMode.No);
                    nf.SetLongValue(Int64.Parse(fieldInfo.Value, CultureInfo.InvariantCulture));
                    return nf;
                case FieldInfoType.SingleField:
                    nf = new NumericField(fieldInfo.Name, store, fieldInfo.Index != IndexingMode.No);
                    nf.SetFloatValue(Single.Parse(fieldInfo.Value, CultureInfo.InvariantCulture));
                    return nf;
                case FieldInfoType.DoubleField:
                    nf = new NumericField(fieldInfo.Name, 8, store, fieldInfo.Index != IndexingMode.No);
                    nf.SetDoubleValue(Double.Parse(fieldInfo.Value, CultureInfo.InvariantCulture));
                    return nf;
                default:
                    throw new SnNotSupportedException("IndexFieldInfo." + fieldInfo.Type);
            }
        }

        private static AbstractField CreateInFolderField(string fieldName, string path)
        {
            var parentPath = RepositoryPath.GetParentPath(path) ?? "/";
            return CreateStringField(fieldName, parentPath, InFolderFieldIndexingInfo);
        }
        private static IEnumerable<AbstractField> CreateInTreeFields(string fieldName, string path)
        {
            var separator = "/";
            string[] fragments = path.ToLowerInvariant().Split(separator.ToCharArray(), StringSplitOptions.None);
            string[] pathSteps = new string[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
                pathSteps[i] = string.Join(separator, fragments, 0, i + 1);
            return pathSteps.Select(p => CreateStringField(fieldName, p, InTreeFieldIndexingInfo)).ToArray();
        }
        private static AbstractField CreateStringField(string name, string value, IPerFieldIndexingInfo indexingInfo)
        {
            var index = EnumConverter.ToLuceneIndexingMode(indexingInfo.IndexingMode);
            var store = EnumConverter.ToLuceneIndexStoringMode(indexingInfo.IndexStoringMode);
            var termVector = EnumConverter.ToLuceneIndexTermVector(indexingInfo.TermVectorStoringMode);

            return new Lucene.Net.Documents.Field(name, value, store, index, termVector);
        }

        private static void ValidateDocumentInfo(IndexDocumentInfo doc)
        {
            ValidateField(doc, IndexFieldName.NodeId);
            ValidateField(doc, IndexFieldName.VersionId);
        }

        private static void ValidateField(IndexDocumentInfo doc, string fieldName)
        {
            var field = doc.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (field == null || string.IsNullOrEmpty(field.Value))
                throw new InvalidOperationException("Invalid empty field value for field: " + fieldName);
        }
    }
}

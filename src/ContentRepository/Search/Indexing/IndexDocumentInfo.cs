using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SnD = SenseNet.Diagnostics;
using SnField = SenseNet.ContentRepository.Field;
using SnS = SenseNet.ContentRepository.Storage;
using SnSS = SenseNet.ContentRepository.Storage.Schema;
using System.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Schema;
using SenseNet.Tools;
using Field = Lucene.Net.Documents.Field;

namespace SenseNet.Search.Indexing
{
    public class IndexDocumentProvider : SenseNet.ContentRepository.Storage.Search.IIndexDocumentProvider
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
    }
    public enum FieldInfoType
    {
        StringField, IntField, LongField, SingleField, DoubleField
    }
    [Serializable]
    [DebuggerDisplay("{Name}:{Type}={Value} | Store:{Store} Index:{Index} TermVector:{TermVector}")]
    public class IndexFieldInfo
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public FieldInfoType Type { get; private set; }
        public Field.Store Store { get; private set; }
        public Field.Index Index { get; private set; }
        public Field.TermVector TermVector { get; private set; }

        public IndexFieldInfo(string name, string value, FieldInfoType type, Field.Store store, Field.Index index, Field.TermVector termVector)
        {
            Name = name;
            Value = value;
            Type = type;
            Store = store;
            Index = index;
            TermVector = termVector;
        }
    }

    [Serializable]
    public class NotIndexedIndexDocumentInfo : IndexDocumentInfo { }
    [Serializable]
    public class IndexDocumentInfo
    {
        [NonSerialized]
        private static PerFieldIndexingInfo __nameFieldIndexingInfo;
        [NonSerialized]
        private static PerFieldIndexingInfo __pathFieldIndexingInfo;
        [NonSerialized]
        private static PerFieldIndexingInfo __inTreeFieldIndexingInfo;
        [NonSerialized]
        private static PerFieldIndexingInfo __inFolderFieldIndexingInfo;

        internal static PerFieldIndexingInfo NameFieldIndexingInfo
        {
            get
            {
                if (__nameFieldIndexingInfo == null)
                {
                    var start = SenseNet.ContentRepository.Schema.ContentTypeManager.Current;
                    __nameFieldIndexingInfo = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(LucObject.FieldName.Name);
                }
                return __nameFieldIndexingInfo;
            }
        }
        internal static PerFieldIndexingInfo PathFieldIndexingInfo
        {
            get
            {
                if (__pathFieldIndexingInfo == null)
                {
                    var start = SenseNet.ContentRepository.Schema.ContentTypeManager.Current;
                    __pathFieldIndexingInfo = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(LucObject.FieldName.Path);
                }
                return __pathFieldIndexingInfo;
            }
        }
        internal static PerFieldIndexingInfo InTreeFieldIndexingInfo
        {
            get
            {
                if (__inTreeFieldIndexingInfo == null)
                {
                    var start = SenseNet.ContentRepository.Schema.ContentTypeManager.Current;
                    __inTreeFieldIndexingInfo = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(LucObject.FieldName.InTree);
                }
                return __inTreeFieldIndexingInfo;
            }
        }
        internal static PerFieldIndexingInfo InFolderFieldIndexingInfo
        {
            get
            {
                if (__inFolderFieldIndexingInfo == null)
                {
                    var start = SenseNet.ContentRepository.Schema.ContentTypeManager.Current;
                    __inFolderFieldIndexingInfo = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(LucObject.FieldName.InFolder);
                }
                return __inFolderFieldIndexingInfo;
            }
        }

        [NonSerialized]
        private static readonly IEnumerable<Document> EmptyDocuments = new Document[0];

        private List<IndexFieldInfo> fields = new List<IndexFieldInfo>();
        public List<IndexFieldInfo> Fields
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

        public void AddField(string name, string value, Field.Store store, Field.Index index)
        {
            AddField(name, value, store, index, Field.TermVector.NO);
        }
        public void AddField(string name, string value, Field.Store store, Field.Index index, Field.TermVector termVector)
        {
            fields.Add(new IndexFieldInfo(name, value, FieldInfoType.StringField, store, index, termVector));
        }
        public void AddField(string name, int value, Field.Store store, bool isIndexed)
        {
            fields.Add(new IndexFieldInfo(name, value.ToString(CultureInfo.InvariantCulture), FieldInfoType.IntField, store, isIndexed ? Field.Index.ANALYZED_NO_NORMS : Field.Index.NO, Field.TermVector.NO));
        }
        public void AddField(string name, long value, Field.Store store, bool isIndexed)
        {
            fields.Add(new IndexFieldInfo(name, value.ToString(CultureInfo.InvariantCulture), FieldInfoType.LongField, store, isIndexed ? Field.Index.ANALYZED_NO_NORMS : Field.Index.NO, Field.TermVector.NO));
        }
        public void AddField(string name, double value, Field.Store store, bool isIndexed)
        {
            fields.Add(new IndexFieldInfo(name, value.ToString(CultureInfo.InvariantCulture), FieldInfoType.DoubleField, store, isIndexed ? Field.Index.ANALYZED_NO_NORMS : Field.Index.NO, Field.TermVector.NO));
        }
        public void AddField(IndexFieldInfo fieldInfo)
        {
            fields.Add(fieldInfo);
        }

        [NonSerialized]
        private static List<string> PostponedFields = new List<string>(new string[] {
            LucObject.FieldName.Name, LucObject.FieldName.Path, LucObject.FieldName.InTree, LucObject.FieldName.InFolder, LucObject.FieldName.Depth, LucObject.FieldName.ParentId,
            LucObject.FieldName.IsSystem
        });
        [NonSerialized]
        private static List<string> ForbiddenFields = new List<string>(new string[] { "Password", "PasswordHash" });
        private static List<string> SkippedMultistepFields = new List<string>(new[] { "Size" });

        private static bool Indexable(Node node)
        {
            var ct = ContentType.GetByName(node.NodeType.Name);
            if (ct == null)
                return true;
            return ct.IndexingEnabled;
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
                doc.AddField(LucObject.FieldName.NodeId, node.Id, Field.Store.YES, true);
                doc.AddField(LucObject.FieldName.VersionId, node.VersionId, Field.Store.YES, true);
                doc.AddField(LucObject.FieldName.Version, node.Version.ToString().ToLowerInvariant(), Field.Store.YES, Field.Index.ANALYZED);
                doc.AddField(LucObject.FieldName.OwnerId, node.OwnerId, Field.Store.YES, true);
                doc.AddField(LucObject.FieldName.CreatedById, node.CreatedById, Field.Store.YES, true);
                doc.AddField(LucObject.FieldName.ModifiedById, node.ModifiedById, Field.Store.YES, true);
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
                    if (node.SavingState != ContentSavingState.Finalized && (field is BinaryField || SkippedMultistepFields.Contains(field.Name)))
                        continue;
                    if (skipBinaries && (field is BinaryField))
                    {
                        if (TextExtractor.TextExtractingWillBePotentiallySlow((BinaryData)((BinaryField)field).GetData()))
                        {
                            doc.HasBinaryField = true;
                            continue;
                        }
                    }

                    IEnumerable<IndexFieldInfo> lucFields = null;
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
            doc.AddField(LucObject.FieldName.IsInherited, isInherited ? BooleanIndexHandler.YES : BooleanIndexHandler.NO, Field.Store.YES, Field.Index.NOT_ANALYZED);
            doc.AddField(LucObject.FieldName.IsMajor, node.Version.IsMajor ? BooleanIndexHandler.YES : BooleanIndexHandler.NO, Field.Store.YES, Field.Index.NOT_ANALYZED);
            doc.AddField(LucObject.FieldName.IsPublic, node.Version.Status == VersionStatus.Approved ? BooleanIndexHandler.YES : BooleanIndexHandler.NO, Field.Store.YES, Field.Index.NOT_ANALYZED);
            doc.AddField(LucObject.FieldName.AllText, textEtract.ToString(), Field.Store.NO, Field.Index.ANALYZED);

            if (faultedFieldNames.Any())
            {
                doc.AddField(LucObject.FieldName.IsFaulted, BooleanIndexHandler.YES , Field.Store.YES, Field.Index.NOT_ANALYZED);
                foreach (var faultedFieldName in faultedFieldNames)
                    doc.AddField(LucObject.FieldName.FaultedFieldName, faultedFieldName, Field.Store.YES, Field.Index.NOT_ANALYZED);
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
                if (fields[i].Name == LucObject.FieldName.AllText)
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
                    if (node.SavingState != ContentSavingState.Finalized && (field is BinaryField || SkippedMultistepFields.Contains(field.Name)))
                        continue;
                    if (!(field is BinaryField))
                        continue;
                    if (!TextExtractor.TextExtractingWillBePotentiallySlow((BinaryData)((BinaryField)field).GetData()))
                        continue;

                    IEnumerable<IndexFieldInfo> lucFields = null;
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

            fields[allTextFieldIndex] = new IndexFieldInfo(LucObject.FieldName.AllText, textEtract.ToString(), FieldInfoType.StringField, Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.NO);

            if (faultedFieldNames.Any())
            {
                if (!this.fields.Any(f => f.Name == LucObject.FieldName.IsFaulted))
                    this.AddField(LucObject.FieldName.IsFaulted, BooleanIndexHandler.YES, Field.Store.YES, Field.Index.NOT_ANALYZED);
                foreach (var faultedFieldName in faultedFieldNames)
                    this.AddField(LucObject.FieldName.FaultedFieldName, faultedFieldName, Field.Store.YES, Field.Index.ANALYZED);
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

            doc.Add(CreateStringField(LucObject.FieldName.Name, RepositoryPath.GetFileName(path), NameFieldIndexingInfo));
            doc.Add(CreateStringField(LucObject.FieldName.Path, path, PathFieldIndexingInfo));

            var nf = new NumericField(LucObject.FieldName.Depth, Field.Store.YES, true);
            nf.SetIntValue(Node.GetDepth(docData.Path));
            doc.Add(nf);


            var fields = CreateInTreeFields(LucObject.FieldName.InTree, docData.Path);
            foreach (var field in fields)
                doc.Add(field);

            doc.Add(CreateInFolderField(LucObject.FieldName.InFolder, path));

            nf = new NumericField(LucObject.FieldName.ParentId, Field.Store.NO, true);
            nf.SetIntValue(docData.ParentId);
            doc.Add(nf);

            doc.Add(new Field(LucObject.FieldName.IsSystem, docData.IsSystem ? BooleanIndexHandler.YES : BooleanIndexHandler.NO, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

            // flags
            doc.Add(new Field(LucObject.FieldName.IsLastPublic, docData.IsLastPublic ? BooleanIndexHandler.YES : BooleanIndexHandler.NO, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
            doc.Add(new Field(LucObject.FieldName.IsLastDraft, docData.IsLastDraft ? BooleanIndexHandler.YES : BooleanIndexHandler.NO, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

            // timestamps
            nf = new NumericField(LucObject.FieldName.NodeTimestamp, Field.Store.YES, true);
            nf.SetLongValue(docData.NodeTimestamp);
            doc.Add(nf);
            nf = new NumericField(LucObject.FieldName.VersionTimestamp, Field.Store.YES, true);
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
        private static AbstractField CreateField(IndexFieldInfo fieldInfo)
        {
            NumericField nf;
            switch (fieldInfo.Type)
            {
                case FieldInfoType.StringField:
                    return new Field(fieldInfo.Name, fieldInfo.Value, fieldInfo.Store, fieldInfo.Index, fieldInfo.TermVector);
                case FieldInfoType.IntField:
                    nf = new NumericField(fieldInfo.Name, fieldInfo.Store, fieldInfo.Index != Field.Index.NO);
                    nf.SetIntValue(Int32.Parse(fieldInfo.Value, CultureInfo.InvariantCulture));
                    return nf;
                case FieldInfoType.LongField:
                    nf = new NumericField(fieldInfo.Name, 8, fieldInfo.Store, fieldInfo.Index != Field.Index.NO);
                    nf.SetLongValue(Int64.Parse(fieldInfo.Value, CultureInfo.InvariantCulture));
                    return nf;
                case FieldInfoType.SingleField:
                    nf = new NumericField(fieldInfo.Name, fieldInfo.Store, fieldInfo.Index != Field.Index.NO);
                    nf.SetFloatValue(Single.Parse(fieldInfo.Value, CultureInfo.InvariantCulture));
                    return nf;
                case FieldInfoType.DoubleField:
                    nf = new NumericField(fieldInfo.Name, 8, fieldInfo.Store, fieldInfo.Index != Field.Index.NO);
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
        private static AbstractField CreateStringField(string name, string value, PerFieldIndexingInfo indexingInfo)
        {
            var index = indexingInfo.IndexingMode ?? PerFieldIndexingInfo.DefaultIndexingMode;
            var store = indexingInfo.IndexStoringMode ?? PerFieldIndexingInfo.DefaultIndexStoringMode;
            var termVector = indexingInfo.TermVectorStoringMode ?? PerFieldIndexingInfo.DefaultTermVectorStoringMode;
            return new Lucene.Net.Documents.Field(name, value, store, index, termVector);
        }

        private static void ValidateDocumentInfo(IndexDocumentInfo doc)
        {
            ValidateField(doc, LucObject.FieldName.NodeId);
            ValidateField(doc, LucObject.FieldName.VersionId);
        }

        private static void ValidateField(IndexDocumentInfo doc, string fieldName)
        {
            var field = doc.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (field == null || string.IsNullOrEmpty(field.Value))
                throw new InvalidOperationException("Invalid empty field value for field: " + fieldName);
        }
    }



    public interface IHasCustomIndexField { }
    public interface ICustomIndexFieldProvider
    {
        IEnumerable<Fieldable> GetFields(SenseNet.ContentRepository.Storage.Data.IndexDocumentData docData);
    }
    internal class CustomIndexFieldManager
    {
        internal static IEnumerable<Fieldable> GetFields(IndexDocumentInfo info, SenseNet.ContentRepository.Storage.Data.IndexDocumentData docData)
        {
            Debug.WriteLine("%> adding custom fields for " + docData.Path);
            return Instance.GetFieldsPrivate(info, docData);
        }

        // -------------------------------------------------------------

        private static object _instanceSync = new object();
        private static CustomIndexFieldManager __instance;
        private static CustomIndexFieldManager Instance
        {
            get
            {
                if (__instance == null)
                {
                    lock (_instanceSync)
                    {
                        if (__instance == null)
                        {
                            var instance = new CustomIndexFieldManager();
                            instance._providers = TypeResolver.GetTypesByInterface(typeof(ICustomIndexFieldProvider))
                                .Select(t => (ICustomIndexFieldProvider)Activator.CreateInstance(t)).ToArray();
                            __instance = instance;
                        }
                    }
                }
                return __instance;
            }
        }

        // ---------------------------------------------------------------------
        
        private IEnumerable<ICustomIndexFieldProvider> _providers;

        private CustomIndexFieldManager() { }

        private IEnumerable<Fieldable> GetFieldsPrivate(IndexDocumentInfo info, IndexDocumentData docData)
        {
            var fields = new List<Fieldable>();
            foreach (var provider in _providers)
            {
                var f = provider.GetFields(docData);
                if (f != null)
                    fields.AddRange(f);
            }
            return fields.Count == 0 ? null : fields;
        }

    }
}

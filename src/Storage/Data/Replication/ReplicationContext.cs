using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Security;
using System.Linq;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class ReplicationContext
{
    public string TypeName { get; set; }
    public bool IsSystemContent { get; set; }

    public int MaxCount { get; set; }
    public int CurrentCount { get; set; }

    public DateTime ReplicationStart { get; set; }
    public DateTime Now { get; set; }

    public int TargetId { get; set; }
    public string TargetPath { get; set; }

    public NodeHeadData NodeHeadData { get; set; }
    public VersionData VersionData { get; set; }
    public DynamicPropertyData DynamicData { get; set; }

    public IndexDocumentData IndexDocumentPrototype { get; set; }
    public IndexDocumentData IndexDocument { get; set; }
    public StringBuilder TextExtract { get; set; }
    public List<IFieldGenerator> FieldGenerators { get; set; }
    private string _paddingFormat;
    public string PaddingFormat => _paddingFormat ??= MaxCount == 0 ? "D" : "D" + Convert.ToInt32(Math.Ceiling(Math.Log10(MaxCount)));

    private readonly DataProvider _dataProvider;
    private readonly IIndexManager _indexManager;
    private readonly ICustomFieldGeneratorFactory _customFieldGeneratorFactory;
    private IUser _currentUser;
    private SnSecurityContext _securityContext;

    /* ================================================================== INITIALIZATION */

    public ReplicationContext(DataProvider dataProvider, IIndexManager indexManager, ICustomFieldGeneratorFactory customFieldGeneratorFactory)
    {
        _dataProvider = dataProvider;
        _indexManager = indexManager;
        _customFieldGeneratorFactory = customFieldGeneratorFactory;
    }

    public void Initialize(ReplicationDescriptor replicationDescriptor, IndexDocumentData indexDocData)
    {
        var indexDocument = _indexManager.CompleteIndexDocument(indexDocData);
        IndexDocumentPrototype = indexDocData;
        FieldGenerators = CreateFieldGenerators(replicationDescriptor, indexDocument, this);
        _currentUser = AccessProvider.Current.GetCurrentUser();
        _securityContext = Providers.Instance.SecurityHandler.CreateSecurityContextFor(_currentUser);
    }
    private static readonly string[] OmittedFieldNames = {
        "NodeId","ParentNodeId","IsSystem","VersionId","NodeTimestamp","VersionTimestamp"
    };
    internal List<IFieldGenerator> CreateFieldGenerators(ReplicationDescriptor replicationDescriptor, IndexDocument indexDocument, ReplicationContext context)
    {
        var result = new List<IFieldGenerator>();
        var fieldNames = indexDocument.Fields.Keys.ToList();

        if (replicationDescriptor.Diversity != null)
        {
            // Create configured field generators
            foreach (var item in replicationDescriptor.Diversity)
            {
                var fieldName = item.Key;
                if (OmittedFieldNames.Contains(fieldName))
                    throw new InvalidOperationException($"The field '{fieldName}' cannot be included in the data generation.");
                var diversity = item.Value;

                fieldNames.Remove(fieldName);
                switch (fieldName)
                {
                    case "Name": result.Add(new NameFieldGenerator(GetDiversity<StringDiversity>(diversity))); break;
                    case "DisplayName": result.Add(new DisplayNameFieldGenerator(GetDiversity<StringDiversity>(diversity))); break;
                    case "Index": result.Add(new IndexFieldGenerator(GetDiversity<IntDiversity>(diversity))); break;
                    case "OwnerId": result.Add(new OwnerIdFieldGenerator(GetDiversity<IntDiversity>(diversity))); break;
                    case "Version": result.Add(new VersionFieldGenerator(GetDiversity<StringDiversity>(diversity))); break;
                    case "CreatedById": result.Add(new CreatedByIdFieldGenerator(GetDiversity<IntDiversity>(diversity))); break;
                    case "ModifiedById": result.Add(new ModifiedByIdFieldGenerator(GetDiversity<IntDiversity>(diversity))); break;
                    case "CreationDate": result.Add(new CreationDateFieldGenerator(GetDiversity<DateTimeDiversity>(diversity))); break;
                    case "ModificationDate": result.Add(new ModificationDateFieldGenerator(GetDiversity<DateTimeDiversity>(diversity))); break;
                    default:
                        {
                            var propertyType = context.DynamicData.PropertyTypes.FirstOrDefault(x => x.Name == fieldName);
                            if (propertyType == null)
                                throw new InvalidOperationException("Unknown property type in the prototype: " + fieldName);

                            if (diversity is ICustomDiversity customDiversity)
                            {
                                result.Add(_customFieldGeneratorFactory.CreateFieldGenerator(propertyType.Name, customDiversity));
                                break;
                            }

                            switch (propertyType.DataType)
                            {
                                case DataType.String:
                                    if (!(diversity is StringDiversity stringDiversity))
                                        throw GetDiversityTypeError<StringDiversity>(diversity, propertyType);
                                    result.Add(new StringFieldGenerator(propertyType.Name, stringDiversity));
                                    break;
                                case DataType.Text:
                                    if (!(diversity is StringDiversity textDiversity))
                                        throw GetDiversityTypeError<StringDiversity>(diversity, propertyType);
                                    result.Add(new TextFieldGenerator(propertyType.Name, textDiversity));
                                    break;
                                case DataType.Int:
                                    if (!(diversity is IntDiversity intDiversity))
                                        throw GetDiversityTypeError<IntDiversity>(diversity, propertyType);
                                    result.Add(new IntFieldGenerator(propertyType.Name, intDiversity));
                                    break;
                                case DataType.DateTime:
                                    if (!(diversity is DateTimeDiversity dateTimeDiversity))
                                        throw GetDiversityTypeError<DateTimeDiversity>(diversity, propertyType);
                                    result.Add(new DateTimeFieldGenerator(propertyType.Name, dateTimeDiversity));
                                    break;
                                case DataType.Currency:
                                case DataType.Binary:
                                case DataType.Reference:
                                    throw new NotSupportedException();
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            break;
                        }
                }
            }
        }

        // Create missing well known field generators
        foreach (var fieldName in fieldNames)
        {
            switch (fieldName)
            {
                case "Name": result.Add(new NameFieldGenerator(null)); break;
                case "DisplayName": result.Add(new DisplayNameFieldGenerator(null)); break;
                case "Index": result.Add(new IndexFieldGenerator(null)); break;
                case "OwnerId": result.Add(new OwnerIdFieldGenerator(null)); break;
                case "Version": result.Add(new VersionFieldGenerator(null)); break;
                case "CreatedById": result.Add(new CreatedByIdFieldGenerator(null)); break;
                case "ModifiedById": result.Add(new ModifiedByIdFieldGenerator(null)); break;
                case "CreationDate": result.Add(new CreationDateFieldGenerator(null)); break;
                case "ModificationDate": result.Add(new ModificationDateFieldGenerator(null)); break;
            }
        }

        return result;
    }
    private static Exception GetDiversityTypeError<TDiv>(IDiversity diversity, PropertyType propertyType) where TDiv : IDiversity
    {
        return new InvalidOperationException($"Cannot use {diversity.GetType().Name} to generate data for the " +
                                             $"'{propertyType.Name}' ({propertyType.DataType}) property. " +
                                             $"Expected: {typeof(TDiv).Name}");
    }
    private static TDiv GetDiversity<TDiv>(IDiversity diversity)
    {
        if (diversity is not TDiv typedDiversity)
            throw new InvalidOperationException(
                $"The DiversitySettings of the Name field should be {typeof(TDiv).Name} instead of {diversity.GetType().Name}.");
        return typedDiversity;
    }

    /* ================================================================== INDEX HANDLING */

    public void SetIndexValue(string fieldName, string value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
        TextExtract.AppendLine(value);
    }
    public void SetIndexValue(string fieldName, string[] value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
        foreach (var item in value)
            TextExtract.AppendLine(item);
    }
    public void SetIndexValue(string fieldName, bool value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, int value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, int[] value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, long value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, float value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, double value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, DateTime value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }

    /* ================================================================== MAIN CONTENT GENERATION METHODS */

    public async Task<int> GenerateContentAsync(CancellationToken cancel)
    {
        Now = DateTime.UtcNow;
        IndexDocument = CreateIndexDocument();
        TextExtract = new StringBuilder();

        var nodeHeadData = NodeHeadData;
        var versionData = VersionData;
        var dynamicData = DynamicData;

        // Generate system data
        nodeHeadData.NodeId = 0;
        nodeHeadData.LastMajorVersionId = 0;
        nodeHeadData.LastMinorVersionId = 0;
        versionData.VersionId = 0;
        versionData.NodeId = 0;
        versionData.ChangedData = null;
        dynamicData.VersionId = 0;

        nodeHeadData.ParentNodeId = TargetId;
        IndexDocument.ParentId = nodeHeadData.ParentNodeId;
        nodeHeadData.IsSystem = IsSystemContent;
        IndexDocument.IsSystem = nodeHeadData.IsSystem;

        // Generate data and index fields
        foreach (var fieldGenerator in FieldGenerators)
            fieldGenerator.Generate(this);

        // INSERT Data
        await _dataProvider.InsertNodeAsync(nodeHeadData, versionData, dynamicData, cancel);

        // INSERT SecurityEntity
        await _securityContext.SecuritySystem.DataProvider.InsertSecurityEntityAsync(new StoredSecurityEntity
        {
            Id = nodeHeadData.NodeId,
            ParentId = TargetId,
            IsInherited = true,
            OwnerId = nodeHeadData.OwnerId
        }, cancel).ConfigureAwait(false);

        // Complete index data
        SetIndexValue(IndexFieldName.NodeId, nodeHeadData.NodeId);
        SetIndexValue(IndexFieldName.VersionId, versionData.VersionId);
        IndexDocument.NodeId = nodeHeadData.NodeId;
        IndexDocument.VersionId = versionData.VersionId;
        IndexDocument.NodeTimestamp = nodeHeadData.Timestamp;
        IndexDocument.VersionTimestamp = versionData.Timestamp;
        IndexDocument.IsLastDraft = true;
        IndexDocument.IsLastPublic = versionData.Version.IsMajor;

        TextExtract.AppendLine(TypeName);
        UpdateTextExtract(IndexDocument, TextExtract);
        IndexDocument.IndexDocumentChanged();

        // Save index document
        var serialized = IndexDocument.IndexDocument.Serialize();
        await _dataProvider.SaveIndexDocumentAsync(versionData.VersionId, serialized, cancel);

        // Write index
        var doc = _indexManager.CompleteIndexDocument(IndexDocument);
        await _indexManager.AddDocumentsAsync(new[] { doc }, cancel);

        CurrentCount++;

        return nodeHeadData.NodeId;
    }
    private IndexDocumentData CreateIndexDocument() =>
        CreateIndexDocument(IndexDocumentPrototype.IndexDocument.Fields.Values);
    private IndexDocumentData CreateIndexDocument(IEnumerable<IndexField> indexFields)
    {
        var indexDoc = new IndexDocument();
        foreach (var f in indexFields)
        {
            IndexField indexField;
            switch (f.Type)
            {
                case IndexValueType.String: indexField = new IndexField(f.Name, f.StringValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.StringArray: indexField = new IndexField(f.Name, f.StringArrayValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.Bool: indexField = new IndexField(f.Name, f.BooleanValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.Int: indexField = new IndexField(f.Name, f.IntegerValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.IntArray: indexField = new IndexField(f.Name, f.IntegerArrayValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.Long: indexField = new IndexField(f.Name, f.LongValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.Float: indexField = new IndexField(f.Name, f.SingleValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.Double: indexField = new IndexField(f.Name, f.DoubleValue, f.Mode, f.Store, f.TermVector); break;
                case IndexValueType.DateTime: indexField = new IndexField(f.Name, f.DateTimeValue, f.Mode, f.Store, f.TermVector); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            indexDoc.Add(indexField);
        }

        return new IndexDocumentData(indexDoc, null);
    }

    private void UpdateTextExtract(IndexDocumentData indexDoc, StringBuilder textExtract)
    {
        var indexField = indexDoc.IndexDocument.Fields[IndexFieldName.AllText];
        indexDoc.IndexDocument.Fields[IndexFieldName.AllText] = new IndexField(IndexFieldName.AllText, textExtract.ToString(),
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
}
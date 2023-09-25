using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Search;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

internal class ReplicationContext
{
    public string TypeName { get; set; }
    public bool IsSystemContent { get; set; }

    public int CountMax { get; set; }
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
    public string PaddingFormat => _paddingFormat ??= CountMax == 0 ? "D" : "D" + Convert.ToInt32(Math.Ceiling(Math.Log10(CountMax)));

    private DataProvider _dataProvider;

    public ReplicationContext(DataProvider dataProvider)
    {
        _dataProvider = dataProvider;
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
        SetIndexValue("IsFolder", true);

        // Generate data and index fields
        foreach (var fieldGenerator in FieldGenerators)
            fieldGenerator.Generate(this);

        // INSERT
        await _dataProvider.InsertNodeAsync(nodeHeadData, versionData, dynamicData, cancel);

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
        var doc = Providers.Instance.IndexManager.CompleteIndexDocument(IndexDocument);
        await Providers.Instance.IndexManager.AddDocumentsAsync(new[] { doc }, cancel);

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
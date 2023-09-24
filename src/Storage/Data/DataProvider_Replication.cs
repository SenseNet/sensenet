using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.Replication;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using String = System.String;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data;

public class ReplicationSettings
{
    public int CountMin { get; set; }
    public int CountMax { get; set; }
    public int MaxItemsPerFolder { get; set; }
    public int MaxFoldersPerFolder { get; set; }
    public IDictionary<string, IDiversity> Diversity { get; set; }
}


public abstract partial class DataProvider
{
    public async Task ReplicateNodeAsync(Node source, Node target, ReplicationSettings replicationSettings, CancellationToken cancel)
    {
        var sourceData = (await LoadNodesAsync(new[] { source.VersionId }, cancel)).FirstOrDefault();
        var sourceIndexDoc = (await LoadIndexDocumentsAsync(new[] { source.VersionId }, cancel)).FirstOrDefault();
        if (sourceData == null || sourceIndexDoc == null)
            throw new InvalidOperationException("Cannot replicate missing source");

        var targetData = (await LoadNodesAsync(new[] { target.VersionId }, cancel)).FirstOrDefault();
        var targetIndexDoc = (await LoadIndexDocumentsAsync(new[] { target.VersionId }, cancel)).FirstOrDefault();
        if (targetData == null || targetIndexDoc == null)
            throw new InvalidOperationException("Cannot replicate missing target");

        var userId = AccessProvider.Current.GetOriginalUser().Id;

        var min = Math.Min(replicationSettings.CountMin, replicationSettings.CountMax);
        var max = Math.Max(replicationSettings.CountMin, replicationSettings.CountMax);
        var count = min >= max ? min : new Random().Next(min, max + 1);

        // Initialize folder generation

        var folderGenContext = new ReplicationContext
        {
            TypeName = target.NodeType.Name.ToLowerInvariant(),
            CountMax = 0, // irrelevant in this case
            ReplicationStart = DateTime.UtcNow,
            IsSystemContent = target.IsSystem, //target.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder"));
            NodeHeadData = targetData.GetNodeHeadData(),
            VersionData = targetData.GetVersionData(),
            DynamicData = targetData.GetDynamicData(true),
            TargetId = target.Id,
            TargetPath = target.Path
        };
        var folderReplicationSettings = new ReplicationSettings
        {
            Diversity = new Dictionary<string, IDiversity>
            {
                {"Name", new StringDiversity {Type = DiversityType.Constant, Pattern = "*"}}
            }
        };
        CreateFieldGenerators(folderReplicationSettings, targetIndexDoc, folderGenContext);
        var folderGenerator = new DefaultFolderGenerator(folderGenContext, count, 3, 2, cancel);

        // Initialize content generation

        var context = new ReplicationContext
        {
            TypeName = source.NodeType.Name.ToLowerInvariant(),
            CountMax = count,
            ReplicationStart = DateTime.UtcNow,
            IsSystemContent = source.IsSystem || target.IsSystem, //source.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder"));
            NodeHeadData = sourceData.GetNodeHeadData(),
            VersionData = sourceData.GetVersionData(),
            DynamicData = sourceData.GetDynamicData(true),
            TargetId = target.Id,
            TargetPath = target.Path
        };

        CreateFieldGenerators(replicationSettings, sourceIndexDoc, context);

        // REPLICATION MAIN ENUMERATION
        for (var i = 0; i < context.CountMax; i++)
        {
            folderGenerator.EnsureFolder();
var i1 = i;
SnTrace.Test.Write(()=>$">>>> {i1} --> {folderGenerator.CurrentFolderId}, {folderGenerator.CurrentFolderPath}");
            context.TargetId = folderGenerator.CurrentFolderId;
            context.TargetPath = folderGenerator.CurrentFolderPath;

            context.CurrentCount = i;

            await GenerateContentAsync(context, cancel);
        }
        await Providers.Instance.IndexManager.CommitAsync(cancel);
    }

    private async Task<int> GenerateContentAsync(ReplicationContext context, CancellationToken cancel)
    {
        var nodeHeadData = context.NodeHeadData;
        var versionData = context.VersionData;
        var dynamicData = context.DynamicData;

        context.Now = DateTime.UtcNow;
        context.IndexDocument = CreateIndexDocument(context);
        context.TextExtract = new StringBuilder();

        // Generate system data
        nodeHeadData.NodeId = 0;
        nodeHeadData.LastMajorVersionId = 0;
        nodeHeadData.LastMinorVersionId = 0;
        versionData.VersionId = 0;
        versionData.NodeId = 0;
        versionData.ChangedData = null;
        dynamicData.VersionId = 0;

        nodeHeadData.ParentNodeId = context.TargetId;
        context.IndexDocument.ParentId = nodeHeadData.ParentNodeId;
        nodeHeadData.IsSystem = context.IsSystemContent;
        context.IndexDocument.IsSystem = nodeHeadData.IsSystem;
        context.SetIndexValue("IsFolder", true);

        // Generate data and index fields
        foreach (var fieldGenerator in context.FieldGenerators)
            fieldGenerator.Generate(context);

        // INSERT
        await InsertNodeAsync(nodeHeadData, versionData, dynamicData, cancel);

        // Complete index data
        context.SetIndexValue(IndexFieldName.NodeId, nodeHeadData.NodeId);
        context.SetIndexValue(IndexFieldName.VersionId, versionData.VersionId);
        context.IndexDocument.NodeId = nodeHeadData.NodeId;
        context.IndexDocument.VersionId = versionData.VersionId;
        context.IndexDocument.NodeTimestamp = nodeHeadData.Timestamp;
        context.IndexDocument.VersionTimestamp = versionData.Timestamp;
        context.IndexDocument.IsLastDraft = true;
        context.IndexDocument.IsLastPublic = versionData.Version.IsMajor;

        context.TextExtract.AppendLine(context.TypeName);
        UpdateTextExtract(context.IndexDocument, context.TextExtract);
        context.IndexDocument.IndexDocumentChanged();

        // Save index document
        var serialized = context.IndexDocument.IndexDocument.Serialize();
        await SaveIndexDocumentAsync(versionData.VersionId, serialized, cancel);

        // Write index
        var doc = Providers.Instance.IndexManager.CompleteIndexDocument(context.IndexDocument);
        await Providers.Instance.IndexManager.AddDocumentsAsync(new[] { doc }, cancel);

        return nodeHeadData.NodeId;
    }

    private IndexDocumentData CreateIndexDocument(ReplicationContext context) =>
        CreateIndexDocument(context.IndexDocumentPrototype.IndexDocument.Fields.Values);
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

    /* =============================== FIELD GENERATOR HELPERS **/

    private static readonly string[] OmittedFieldNames = new[]
    {
        "NodeId","ParentNodeId","IsSystem","VersionId","NodeTimestamp","VersionTimestamp"
    };
    private static readonly string[] WellKnownFieldNames = new[]
    {
        "Name", "DisplayName", "Index", "OwnerId", "Version", "CreatedById", "ModifiedById", "CreationDate", "ModificationDate",
    };
    private static readonly string[] WellKnownIndexFieldNames = new[]
    {
        IndexFieldName.NodeId, IndexFieldName.Name, IndexFieldName.Path, IndexFieldName.InTree, IndexFieldName.InFolder, "IsFolder", 
        IndexFieldName.CreatedById, "CreatedBy", IndexFieldName.ModifiedById, "ModifiedBy", IndexFieldName.OwnerId, "Owner",
        IndexFieldName.Depth, IndexFieldName.ParentId, IndexFieldName.IsSystem,
        IndexFieldName.VersionId, IndexFieldName.IsLastPublic, IndexFieldName.IsLastDraft,
        IndexFieldName.Version, IndexFieldName.IsMajor, IndexFieldName.IsPublic,
        IndexFieldName.NodeTimestamp, IndexFieldName.VersionTimestamp,
        "Workspace",
    };
    private List<IFieldGenerator> CreateFieldGenerators(ReplicationSettings replicationSettings, IndexDocumentData indexDocumentData, ReplicationContext context)
    {
        var result = new List<IFieldGenerator>();
        var indexDocument = Providers.Instance.IndexManager.CompleteIndexDocument(indexDocumentData);
        var fieldNames = indexDocument.Fields.Keys.ToList();

        // Create configured well known field generators
        foreach (var item in replicationSettings.Diversity)
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

        // Create index document prototype

        //fieldNames = fieldNames.Except(_wellKnownFieldNames).Except(_wellKnownIndexFieldNames).ToList();
        //var protoTypeFields = indexDocument.Fields;
        //foreach (var unnecessaryField in indexDocument.Fields.Keys.ToArray())
        //{
        //    if (protoTypeFields.ContainsKey(unnecessaryField))
        //        protoTypeFields.Remove(unnecessaryField);
        //}
        //context.IndexDocumentPrototype = new IndexDocumentData(indexDocument, null)
        //{
        //    NodeTypeId = indexDocumentData.NodeTypeId
        //};
        context.IndexDocumentPrototype = indexDocumentData;
        context.FieldGenerators = result;

        return result;
    }
    private Exception GetDiversityTypeError<TDiv>(IDiversity diversity, PropertyType propertyType) where TDiv : IDiversity
    {
        return new InvalidOperationException($"Cannot use {diversity.GetType().Name} to generate data for the " +
                                             $"'{propertyType.Name}' ({propertyType.DataType}) property. " +
                                             $"Expected: {typeof(TDiv).Name}");
    }
    private Tdiv GetDiversity<Tdiv>(IDiversity diversity)
    {
        if (diversity is not Tdiv typedDiversity)
            throw new InvalidOperationException(
                $"The DiversitySettings of the Name field should be {typeof(Tdiv).Name} instead of {diversity.GetType().Name}.");
        return typedDiversity;
    }

}
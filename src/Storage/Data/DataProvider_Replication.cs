using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.Replication;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data;

public class ReplicationSettings
{
    public int CountMax { get; set; }
    public int MaxItemsPerFolder { get; set; }
    public int MaxFoldersPerFolder { get; set; }
    public int FirstFolderIndex { get; set; }
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

        var count = replicationSettings.CountMax;

        // Initialize folder generation

        var folderGenContext = new ReplicationContext(this)
        {
            TypeName = target.NodeType.Name.ToLowerInvariant(),
            CountMax = replicationSettings.CountMax, // 
            ReplicationStart = DateTime.UtcNow,
            IsSystemContent = target.IsSystem, //target.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder")),
            NodeHeadData = targetData.GetNodeHeadData(),
            VersionData = targetData.GetVersionData(),
            DynamicData = targetData.GetDynamicData(true),
            TargetId = target.Id,
            TargetPath = target.Path
        };
        var folderReplicationSettings = new ReplicationSettings
        {
            //Diversity = new Dictionary<string, IDiversity>()
            //{
            //    {"Name", new StringDiversity {Type = DiversityType.Constant, Pattern = "*"}}
            //}
        };
        CreateFieldGenerators(folderReplicationSettings, targetIndexDoc, folderGenContext);
        var folderGenerator = new DefaultFolderGenerator(folderGenContext,
            replicationSettings.CountMax,
            replicationSettings.FirstFolderIndex,
            replicationSettings.MaxItemsPerFolder, replicationSettings.MaxFoldersPerFolder, cancel);

        // Initialize content generation

        var context = new ReplicationContext(this)
        {
            TypeName = source.NodeType.Name.ToLowerInvariant(),
            CountMax = replicationSettings.CountMax,
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
            await folderGenerator.EnsureFolderAsync(cancel);
var i1 = i;
SnTrace.Test.Write(()=>$">>>> {i1} --> {folderGenerator.CurrentFolderId}, {folderGenerator.CurrentFolderPath}");
            context.TargetId = folderGenerator.CurrentFolderId;
            context.TargetPath = folderGenerator.CurrentFolderPath;

            await context.GenerateContentAsync(cancel);
        }
        await Providers.Instance.IndexManager.CommitAsync(cancel);
    }
    
    /* =============================== FIELD GENERATOR HELPERS **/

    private static readonly string[] OmittedFieldNames = {
        "NodeId","ParentNodeId","IsSystem","VersionId","NodeTimestamp","VersionTimestamp"
    };
    //private static readonly string[] WellKnownFieldNames = {
    //    "Name", "DisplayName", "Index", "OwnerId", "Version", "CreatedById", "ModifiedById", "CreationDate", "ModificationDate",
    //};
    //private static readonly string[] WellKnownIndexFieldNames = {
    //    IndexFieldName.NodeId, IndexFieldName.Name, IndexFieldName.Path, IndexFieldName.InTree, IndexFieldName.InFolder, "IsFolder", 
    //    IndexFieldName.CreatedById, "CreatedBy", IndexFieldName.ModifiedById, "ModifiedBy", IndexFieldName.OwnerId, "Owner",
    //    IndexFieldName.Depth, IndexFieldName.ParentId, IndexFieldName.IsSystem,
    //    IndexFieldName.VersionId, IndexFieldName.IsLastPublic, IndexFieldName.IsLastDraft,
    //    IndexFieldName.Version, IndexFieldName.IsMajor, IndexFieldName.IsPublic,
    //    IndexFieldName.NodeTimestamp, IndexFieldName.VersionTimestamp,
    //    "Workspace",
    //};
    private void CreateFieldGenerators(ReplicationSettings replicationSettings, IndexDocumentData indexDocumentData, ReplicationContext context)
    {
        var result = new List<IFieldGenerator>();
        var indexDocument = Providers.Instance.IndexManager.CompleteIndexDocument(indexDocumentData);
        var fieldNames = indexDocument.Fields.Keys.ToList();

        if (replicationSettings.Diversity != null)
        {
            // Create configured field generators
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

        context.IndexDocumentPrototype = indexDocumentData;
        context.FieldGenerators = result;
    }
    private Exception GetDiversityTypeError<TDiv>(IDiversity diversity, PropertyType propertyType) where TDiv : IDiversity
    {
        return new InvalidOperationException($"Cannot use {diversity.GetType().Name} to generate data for the " +
                                             $"'{propertyType.Name}' ({propertyType.DataType}) property. " +
                                             $"Expected: {typeof(TDiv).Name}");
    }
    private TDiv GetDiversity<TDiv>(IDiversity diversity)
    {
        if (diversity is not TDiv typedDiversity)
            throw new InvalidOperationException(
                $"The DiversitySettings of the Name field should be {typeof(TDiv).Name} instead of {diversity.GetType().Name}.");
        return typedDiversity;
    }

}
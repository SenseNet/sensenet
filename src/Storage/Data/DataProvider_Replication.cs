using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using String = System.String;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data;

public enum DiversityType { Constant, Sequence, Random, /* Dictionary, etc*/}

//public class DiversitySettings
//{
//    public DiversityType Type { get; set; }
//}
//public class StringDiversitySettings : DiversitySettings
//{
//    public string Pattern { get; set; }
//}
//public class IntDiversitySettings : DiversitySettings
//{
//    public int MinValue { get; set; }
//    public int MaxValue { get; set; }
//}
//public class ReferenceIdDiversitySettings : IntDiversitySettings
//{
//}
//public class DateTimeDiversitySettings : DiversitySettings
//{
//    public int MinValue { get; set; }
//    public int MaxValue { get; set; }
//    public Func<DateTime> Value { get; set; }
//}
//public class ReplicationSettings
//{
//    public int CountMin { get; set; }
//    public int CountMax { get; set; }
//    public IDictionary<string, DiversitySettings> Diversity { get; set; }
//}

public class Sequence
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
}
public class DateTimeSequence
{
    // How many seconds is int.max: 2147483647 / 60 / 60 / 24 / 365 = 68.09625973490614
    public DateTime MinValue { get; set; }
    public DateTime MaxValue { get; set; }
    public TimeSpan Step { get; set; } = TimeSpan.FromSeconds(1);
}

public interface IDiversity
{
    DiversityType Type { get; set; }
    Type DataType { get; set; }
    object Current { get; set; }
}
public interface IDiversity<T> : IDiversity
{
    new T Current { get; set; }
}
public abstract class Diversity<T> : IDiversity<T>
{
    public DiversityType Type { get; set; }
    public Type DataType { get; set; }
    public T Current { get; set; }

    object IDiversity.Current
    {
        get => Current;
        set => Current = (T)value;
    }
}
public class StringDiversity : Diversity<string>
{
    public string Pattern { get; set; }
    public Sequence Sequence { get; set; }
}
public class IntDiversity : Diversity<int>
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
}
public class ReferenceIdDiversity : IntDiversity
{
}
public class DateTimeDiversity : Diversity<DateTime>
{
    public DateTimeSequence Sequence { get; set; }
}

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
    private class ReplicationContext
    {
        public int CountMin { get; set; }
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
    }


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

        // Initialize folder generation

        var folderHeadData = sourceData.GetNodeHeadData();
        var folderVersionData = sourceData.GetVersionData();
        var folderDynamicData = sourceData.GetDynamicData(true);

        var folderGenContext = new ReplicationContext
        {
            CountMin = replicationSettings.CountMin,
            CountMax = replicationSettings.CountMax,
            ReplicationStart = DateTime.UtcNow,
            NodeHeadData = folderHeadData,
            VersionData = folderVersionData,
            DynamicData = folderDynamicData,
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
        var folderFieldGenerators = CreateFieldGenerators(folderReplicationSettings, targetIndexDoc, folderGenContext);

        var folderGenerator = new FolderGenerator1(folderGenContext, 0, 3, 2, cancel);

        // Initialize content generation

        var typeName = source.NodeType.Name.ToLowerInvariant();

        var nodeHeadData = sourceData.GetNodeHeadData();
        var versionData = sourceData.GetVersionData();
        var dynamicData = sourceData.GetDynamicData(true);

        var sourceIsSystem = source.NodeType.IsInstaceOfOrDerivedFrom(NodeType.GetByName("SystemFolder"));
        var min = Math.Min(replicationSettings.CountMin, replicationSettings.CountMax);
        var max = Math.Max(replicationSettings.CountMin, replicationSettings.CountMax);
        var count = min >= max ? min : new Random().Next(min, max + 1);
        var context = new ReplicationContext
        {
            CountMin = replicationSettings.CountMin,
            CountMax = replicationSettings.CountMax,
            ReplicationStart = DateTime.UtcNow,
            NodeHeadData = nodeHeadData,
            VersionData = versionData,
            DynamicData = dynamicData,
            TargetId = target.Id,
            TargetPath = target.Path
        };
        var fieldGenerators = CreateFieldGenerators(replicationSettings, sourceIndexDoc, context);
        for (var i = 0; i < count; i++)
        {
            folderGenerator.EnsureFolder();
            SnTrace.Test.Write(()=>$"{i} --> {folderGenerator.CurrentFolderId}, {folderGenerator.CurrentFolderPath}");
            context.TargetId = folderGenerator.CurrentFolderId;
            context.TargetPath = folderGenerator.CurrentFolderPath;

            context.CurrentCount = i;

            await XxxAsync(context, fieldGenerators, target.IsSystem || sourceIsSystem, typeName, cancel);


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
            nodeHeadData.IsSystem = target.IsSystem || sourceIsSystem;
            context.IndexDocument.IsSystem = nodeHeadData.IsSystem;
            SetIndexValue("IsFolder", true, context);

            // Generate data and index fields
            foreach (var fieldGenerator in fieldGenerators)
                fieldGenerator.Generate(context);

            // INSERT
            await InsertNodeAsync(nodeHeadData, versionData, dynamicData, cancel);

            // Complete index data
            SetIndexValue(IndexFieldName.NodeId, nodeHeadData.NodeId, context);
            SetIndexValue(IndexFieldName.VersionId, versionData.VersionId, context);
            context.IndexDocument.NodeId = nodeHeadData.NodeId;
            context.IndexDocument.VersionId = versionData.VersionId;
            context.IndexDocument.NodeTimestamp = nodeHeadData.Timestamp;
            context.IndexDocument.VersionTimestamp = versionData.Timestamp;
            context.IndexDocument.IsLastDraft = true;
            context.IndexDocument.IsLastPublic = versionData.Version.IsMajor;

            context.TextExtract.AppendLine(typeName);
            UpdateTextExtract(context.IndexDocument, context.TextExtract);
            context.IndexDocument.IndexDocumentChanged();

            // Save index document
            var serialized = context.IndexDocument.IndexDocument.Serialize();
            await SaveIndexDocumentAsync(versionData.VersionId, serialized, cancel);

            // Write index
            var doc = Providers.Instance.IndexManager.CompleteIndexDocument(context.IndexDocument);
            await Providers.Instance.IndexManager.AddDocumentsAsync(new[] { doc}, cancel);
        }
        await Providers.Instance.IndexManager.CommitAsync(cancel);
    }

    private async Task XxxAsync(ReplicationContext context, List<IFieldGenerator> fieldGenerators, bool targetIsSystem, string typeName, CancellationToken cancel)
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
        nodeHeadData.IsSystem = targetIsSystem;
        context.IndexDocument.IsSystem = nodeHeadData.IsSystem;
        SetIndexValue("IsFolder", true, context);

        // Generate data and index fields
        foreach (var fieldGenerator in fieldGenerators)
            fieldGenerator.Generate(context);

        // INSERT
        await InsertNodeAsync(nodeHeadData, versionData, dynamicData, cancel);

        // Complete index data
        SetIndexValue(IndexFieldName.NodeId, nodeHeadData.NodeId, context);
        SetIndexValue(IndexFieldName.VersionId, versionData.VersionId, context);
        context.IndexDocument.NodeId = nodeHeadData.NodeId;
        context.IndexDocument.VersionId = versionData.VersionId;
        context.IndexDocument.NodeTimestamp = nodeHeadData.Timestamp;
        context.IndexDocument.VersionTimestamp = versionData.Timestamp;
        context.IndexDocument.IsLastDraft = true;
        context.IndexDocument.IsLastPublic = versionData.Version.IsMajor;

        context.TextExtract.AppendLine(typeName);
        UpdateTextExtract(context.IndexDocument, context.TextExtract);
        context.IndexDocument.IndexDocumentChanged();

        // Save index document
        var serialized = context.IndexDocument.IndexDocument.Serialize();
        await SaveIndexDocumentAsync(versionData.VersionId, serialized, cancel);

        // Write index
        var doc = Providers.Instance.IndexManager.CompleteIndexDocument(context.IndexDocument);
        await Providers.Instance.IndexManager.AddDocumentsAsync(new[] { doc }, cancel);

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


    private static readonly Random _rng = new Random();
    private static string _generate(ReplicationContext context, StringDiversity diversity)
    {
        var pattern = diversity.Pattern;
        var paddingFormat = "D" + Convert.ToInt32(Math.Ceiling(Math.Log10(context.CountMax)));
        Func<int, string> Replace = (i) => pattern.Replace("*", i.ToString(paddingFormat));

        var min = diversity.Sequence?.MinValue ?? 0;
        var max = diversity.Sequence?.MaxValue ?? 0;
        switch (diversity.Type)
        {
            case DiversityType.Constant:
                return pattern;

            case DiversityType.Sequence:
                if (max == min)
                    return Replace(min);
                if (max < min)
                    return Replace(context.CurrentCount + min);
                var offset = context.CurrentCount % (max - min + 1);
                return Replace(min + offset);

            case DiversityType.Random:
                if(min >= max)
                    return Replace(min);
                return Replace(_rng.Next(min, max + 1));
                //return RandomNumberGenerator.GetInt32(min, max + 1);

            default: throw new ArgumentOutOfRangeException();
        }
    }
    private static int _generate(ReplicationContext context, IntDiversity diversity)
    {
        var min = diversity.MinValue;
        var max = diversity.MaxValue;

        switch (diversity.Type)
        {
            case DiversityType.Constant:
                return min;

            case DiversityType.Sequence:
                if (max == min)
                    return min;
                if (max < min)
                    return context.CurrentCount + min;
                var offset = context.CurrentCount % (max - min + 1);
                return min + offset;

            case DiversityType.Random:
                return min >= max ? min : _rng.Next(min, max + 1);
            //return RandomNumberGenerator.GetInt32(min, max + 1);

            default: throw new ArgumentOutOfRangeException();
        }
    }
    private static DateTime _generate(ReplicationContext context, DateTimeDiversity diversity)
    {
        long LongRandom(long min, long max, Random rand) // See: https://stackoverflow.com/questions/6651554/random-number-in-long-range-is-this-the-way
        {
            var buf = new byte[8];
            rand.NextBytes(buf);
            var longRand = BitConverter.ToInt64(buf, 0);
            return (Math.Abs(longRand % (max - min)) + min);
        }

        var min = diversity.Sequence.MinValue;
        var max = diversity.Sequence.MaxValue;
        var step = diversity.Sequence.Step;
        if (diversity.Current == default)
            diversity.Current = min;

        switch (diversity.Type)
        {
            case DiversityType.Constant:
                return min;

            case DiversityType.Sequence:
                if (max == min)
                    return min;
                if (max < min)
                    return diversity.Current += step;
                var d = diversity.Current + step;
                if (d > max)
                    d = min;
                diversity.Current = d;
                return d;

            case DiversityType.Random:
                if (min >= max)
                    return min;
                return new DateTime(LongRandom(min.Ticks, max.Ticks + 1, _rng));

            default: throw new ArgumentOutOfRangeException();
        }
    }


    private static void SetIndexValue(string fieldName, string value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
        context.TextExtract.AppendLine(value);
    }
    private static void SetIndexValue(string fieldName, string[] value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
        foreach (var item in value)
            context.TextExtract.AppendLine(item);
    }
    private static void SetIndexValue(string fieldName, bool value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    private static void SetIndexValue(string fieldName, int value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    private static void SetIndexValue(string fieldName, int[] value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    private static void SetIndexValue(string fieldName, long value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    private static void SetIndexValue(string fieldName, float value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    private static void SetIndexValue(string fieldName, double value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    private static void SetIndexValue(string fieldName, DateTime value, ReplicationContext context)
    {
        var indexField = context.IndexDocument.IndexDocument.Fields[fieldName];
        context.IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }

    private void UpdateTextExtract(IndexDocumentData indexDoc, StringBuilder textExtract)
    {
        var indexField = indexDoc.IndexDocument.Fields[IndexFieldName.AllText];
        indexDoc.IndexDocument.Fields[IndexFieldName.AllText] = new IndexField(IndexFieldName.AllText, textExtract.ToString(),
            indexField.Mode, indexField.Store, indexField.TermVector);
    }


    private T GetAndRemoveDiversityField<T>(string fieldName, IDictionary<string, IDiversity> diversityInput)
    {
        if (!diversityInput.TryGetValue(fieldName, out var settings))
            return default;

        if (settings is not T typedSettings)
            throw new InvalidOperationException(
                $"The DiversitySettings of the Name field should be {typeof(T).Name} instead of {settings.GetType().Name}.");
        diversityInput.Remove(fieldName);
        return typedSettings;
    }



    /* ================================================================================= */

    private interface IFolderGenerator
    {
    }

    private class FolderGenerator1 : IFolderGenerator
    {
        private ReplicationContext _context;
        private CancellationToken _cancel;

        public int RootFolderId { get; }
        public string RootFolderPath { get; }
        public int MaxItems { get; }
        public int MaxItemsPerFolder { get; }
        public int MaxFoldersPerFolder { get; }

        public int CurrentFolderId { get; private set; }
        public string CurrentFolderPath { get; private set; }

        public FolderGenerator1(ReplicationContext context, int maxItems, int maxItemsPerFolder, int maxFoldersPerFolder, CancellationToken cancel)
        {
            _context = context;
            RootFolderId = context.TargetId;
            RootFolderPath = context.TargetPath;
            MaxItems = maxItems;
            MaxItemsPerFolder = maxItemsPerFolder;
            MaxFoldersPerFolder = maxFoldersPerFolder;
            _cancel = cancel;

            var maxLevels = Convert.ToInt32(Math.Ceiling(Math.Log(Math.Ceiling(0.0d + MaxItems / MaxItemsPerFolder),
                maxFoldersPerFolder)));
            _ids = new int[maxLevels];
            _names = new string[maxLevels];
            _levels = new int[maxLevels];
            for (int i = 0; i < _levels.Length; i++)
                _levels[i] = maxFoldersPerFolder - 1;
        }

        private readonly int[] _ids;
        private readonly string[] _names;
        private readonly int[] _levels;
        private int _itemIndex = -1;
        public void EnsureFolder()
        {
            if (++_itemIndex % MaxItemsPerFolder != 0)
                return;
            CreateLevel(0);
        }

        private void CreateLevel(int level)
        {
            if (++_levels[level] >= MaxFoldersPerFolder)
            {
                if(level < _levels.Length - 1)
                    CreateLevel(level + 1);
                _levels[level] = 0;
            }
            CreateFolder(level);
        }

        private void CreateFolder(int level)
        {
            var parentId = level == _levels.Length - 1 ? RootFolderId : _ids[level + 1];
            var parentPath = level == _levels.Length - 1 ? RootFolderPath : GetPath(level + 1);
            var nodeId = GenerateDataAndIndex(parentId, parentPath, _itemIndex.ToString());

            var name = _itemIndex.ToString();
            _ids[level] = nodeId;
            _names[level] = name;
            CurrentFolderId = nodeId;
            CurrentFolderPath = parentPath + "/" + name;
        }

        private string GetPath(int level)
        {
            var path = RootFolderPath;
            for (var i = _levels.Length - 1; i >= level; i--)
                path = path + "/" + _names[i];
            return path;
        }

        private int _lastId = 100_000_000;
        private int GenerateDataAndIndex(int parentId, string parentPath, string name)
        {
            ???
            return ++_lastId;
            throw new NotImplementedException();
        }
    }

    /* --------------------------------------------------------------------------------- */

    private interface IFieldGenerator
    {
        void Generate(ReplicationContext context);
    }

    private class NameFieldGenerator : IFieldGenerator
    {
        private readonly StringDiversity _diversity;
        public NameFieldGenerator(StringDiversity diversity)
        {
            _diversity = diversity ?? new StringDiversity{ Type = DiversityType.Sequence, Sequence = new Sequence{ MinValue = 1}};
        }
        public void Generate(ReplicationContext context)
        {
            context.NodeHeadData.Name = _generate(context, _diversity);
            context.NodeHeadData.Path = RepositoryPath.Combine(context.TargetPath, context.NodeHeadData.Name); // consider generated folder structure
            context.IndexDocument.Path = context.NodeHeadData.Path;
            context.TextExtract.AppendLine(context.NodeHeadData.Name.ToLowerInvariant());
        }
    }
    private class DisplayNameFieldGenerator : IFieldGenerator
    {
        private readonly StringDiversity _diversity;
        public DisplayNameFieldGenerator(StringDiversity diversity)
        {
            _diversity = diversity;
        }
        public void Generate(ReplicationContext context)
        {
            if (_diversity == null)
                return;
            var value = _generate(context, _diversity);
            context.NodeHeadData.DisplayName = value;
            if (value == null)
                return;
            var lowerValue = value.ToLowerInvariant();
            SetIndexValue(IndexFieldName.DisplayName, lowerValue, context);
            context.TextExtract.AppendLine(lowerValue);
        }
    }
    private class IndexFieldGenerator : IFieldGenerator
    {
        private readonly IntDiversity _diversity;
        public IndexFieldGenerator(IntDiversity diversity)
        {
            _diversity = diversity ?? new IntDiversity {Type = DiversityType.Constant, MinValue = 0};
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            context.NodeHeadData.Index = value;
            SetIndexValue(IndexFieldName.Index, value, context);
        }
    }
    private class OwnerIdFieldGenerator : IFieldGenerator
    {
        private readonly IntDiversity _diversity;
        public OwnerIdFieldGenerator(IntDiversity diversity)
        {
            _diversity = diversity ?? new IntDiversity { Type = DiversityType.Constant, MinValue = 1 }; // Admin Id
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            context.NodeHeadData.OwnerId = value;
            SetIndexValue(IndexFieldName.OwnerId, value, context);
            SetIndexValue("Owner", new[] { value }, context); // From original index doc
        }
    }
    private class VersionFieldGenerator : IFieldGenerator
    {
        private readonly StringDiversity _diversity;
        public VersionFieldGenerator(StringDiversity diversity)
        {
            _diversity = diversity ?? new StringDiversity { Type = DiversityType.Constant, Pattern = "v1.0.a" }; ;
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            var version = VersionNumber.Parse(value);
            context.VersionData.Version = version;
            if (value == null)
                return;
            var lowerValue = value.ToLowerInvariant();
            SetIndexValue(IndexFieldName.Version, lowerValue, context);
            SetIndexValue(IndexFieldName.IsMajor, version.IsMajor, context);
            SetIndexValue(IndexFieldName.IsPublic, version.Status == VersionStatus.Approved, context);
            context.TextExtract.AppendLine(lowerValue);
        }
    }
    private class CreatedByIdFieldGenerator : IFieldGenerator
    {
        private readonly IntDiversity _diversity;
        public CreatedByIdFieldGenerator(IntDiversity diversity)
        {
            _diversity = diversity ?? new IntDiversity { Type = DiversityType.Constant, MinValue = 1 }; // Admin Id
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            context.NodeHeadData.CreatedById = value;
            context.VersionData.CreatedById = value;
            SetIndexValue(IndexFieldName.CreatedById, value, context);
            SetIndexValue("CreatedBy", new[] { value }, context); // From original index doc
        }
    }
    private class ModifiedByIdFieldGenerator : IFieldGenerator
    {
        private readonly IntDiversity _diversity;
        public ModifiedByIdFieldGenerator(IntDiversity diversity)
        {
            _diversity = diversity ?? new IntDiversity { Type = DiversityType.Constant, MinValue = 1 }; // Admin Id
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            context.NodeHeadData.ModifiedById = value;
            context.VersionData.ModifiedById = value;
            SetIndexValue(IndexFieldName.ModifiedById, value, context);
            SetIndexValue("ModifiedBy", new[] { value }, context); // From original index doc
        }
    }
    private class CreationDateFieldGenerator : IFieldGenerator
    {
        private readonly DateTimeDiversity _diversity;
        public CreationDateFieldGenerator(DateTimeDiversity diversity)
        {
            _diversity = diversity;
        }
        public void Generate(ReplicationContext context)
        {
            var value = _diversity != null ? _generate(context, _diversity) : context.Now;
            context.NodeHeadData.CreationDate = value;
            context.VersionData.CreationDate = value;
            SetIndexValue(IndexFieldName.CreationDate, value, context);
        }
    }
    private class ModificationDateFieldGenerator : IFieldGenerator
    {
        private readonly DateTimeDiversity _diversity;
        public ModificationDateFieldGenerator(DateTimeDiversity diversity)
        {
            _diversity = diversity;
        }
        public void Generate(ReplicationContext context)
        {
            var value = _diversity != null ? _generate(context, _diversity) : context.Now;
            context.NodeHeadData.ModificationDate = value;
            context.VersionData.ModificationDate = value;
            SetIndexValue(IndexFieldName.ModificationDate, value, context);
        }
    }

    /* ========================================================================= DYNAMIC FIELD GENERATORS */

    private class StringFieldGenerator : IFieldGenerator
    {
        private readonly StringDiversity _diversity;
        public PropertyType PropertyType { get; }
        public StringFieldGenerator(PropertyType propertyType, StringDiversity diversity)
        {
            PropertyType = propertyType;
            _diversity = diversity;
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            StoreData(value, context);
            if (value == null)
                return;
            var lowerValue = value.ToLowerInvariant();
            SetIndexValue(PropertyType.Name, lowerValue, context);
            context.TextExtract.AppendLine(lowerValue);
        }
        protected virtual void StoreData(string value, ReplicationContext context)
        {
            context.DynamicData.DynamicProperties[PropertyType] = value;
        }
    }
    private class TextFieldGenerator : StringFieldGenerator
    {
        public TextFieldGenerator(PropertyType propertyType, StringDiversity diversity) : base(propertyType, diversity) { }
        protected override void StoreData(string value, ReplicationContext context)
        {
            context.DynamicData.LongTextProperties[PropertyType] = value;
        }
    }
    private class IntFieldGenerator : IFieldGenerator
    {
        private readonly IntDiversity _diversity;
        public PropertyType PropertyType { get; }
        public IntFieldGenerator(PropertyType propertyType, IntDiversity diversity)
        {
            PropertyType = propertyType;
            _diversity = diversity;
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            context.DynamicData.DynamicProperties[PropertyType] = value;
            SetIndexValue(PropertyType.Name, value, context);
        }
    }
    private class DateTimeFieldGenerator : IFieldGenerator
    {
        private readonly DateTimeDiversity _diversity;
        public PropertyType PropertyType { get; }
        public DateTimeFieldGenerator(PropertyType propertyType, DateTimeDiversity diversity)
        {
            PropertyType = propertyType;
            _diversity = diversity;
        }
        public void Generate(ReplicationContext context)
        {
            var value = _generate(context, _diversity);
            context.DynamicData.DynamicProperties[PropertyType] = value;
            SetIndexValue(PropertyType.Name, value, context);
        }
    }

    private static readonly string[] _omittedFieldNames = new[]
    {
        "NodeId","ParentNodeId","IsSystem","VersionId","NodeTimestamp","VersionTimestamp"
    };
    private static readonly string[] _wellKnownFieldNames = new[]
    {
        "Name", "DisplayName", "Index", "OwnerId", "Version", "CreatedById", "ModifiedById", "CreationDate", "ModificationDate",
    };
    private static readonly string[] _wellKnownIndexFieldNames = new[]
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
            if (_omittedFieldNames.Contains(fieldName))
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
                            result.Add(new StringFieldGenerator(propertyType, stringDiversity));
                            break;
                        case DataType.Text:
                            if (!(diversity is StringDiversity textDiversity))
                                throw GetDiversityTypeError<StringDiversity>(diversity, propertyType);
                            result.Add(new TextFieldGenerator(propertyType, textDiversity));
                            break;
                        case DataType.Int:
                            if (!(diversity is IntDiversity intDiversity))
                                throw GetDiversityTypeError<IntDiversity>(diversity, propertyType);
                            result.Add(new IntFieldGenerator(propertyType, intDiversity));
                            break;
                        case DataType.DateTime:
                            if (!(diversity is DateTimeDiversity dateTimeDiversity))
                                throw GetDiversityTypeError<DateTimeDiversity>(diversity, propertyType);
                            result.Add(new DateTimeFieldGenerator(propertyType, dateTimeDiversity));
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
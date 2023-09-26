using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

internal interface IFieldGenerator
{
    void Generate(ReplicationContext context);
}

internal abstract class FieldGenerator : IFieldGenerator
{
    public abstract void Generate(ReplicationContext context);

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
    internal static void CreateFieldGenerators(ReplicationDescriptor replicationDescriptor, IndexDocumentData indexDocumentData, ReplicationContext context)
    {
        var result = new List<IFieldGenerator>();
        var indexDocument = Providers.Instance.IndexManager.CompleteIndexDocument(indexDocumentData);
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

}
internal class NameFieldGenerator : StringFieldGenerator
{
    public NameFieldGenerator(StringDiversity diversity) : base("Name", diversity)
    {
        Diversity = diversity ?? new StringDiversity { Type = DiversityType.Sequence, Sequence = new Sequence { MinValue = 1 } };
    }
    public override void Generate(ReplicationContext context)
    {
        context.NodeHeadData.Name = Generate(context, Diversity);
        context.NodeHeadData.Path = RepositoryPath.Combine(context.TargetPath, context.NodeHeadData.Name); // consider generated folder structure
        context.IndexDocument.Path = context.NodeHeadData.Path;
        context.TextExtract.AppendLine(context.NodeHeadData.Name.ToLowerInvariant());
    }
}
internal class FolderGeneratorNameFieldGenerator : StringFieldGenerator
{
    public string Name { get; set; }
    public FolderGeneratorNameFieldGenerator() : base("Name", null) { }
    public override void Generate(ReplicationContext context)
    {
        context.NodeHeadData.Name = Name;
        context.NodeHeadData.Path = RepositoryPath.Combine(context.TargetPath, context.NodeHeadData.Name); // consider generated folder structure
        context.IndexDocument.Path = context.NodeHeadData.Path;
        context.TextExtract.AppendLine(Name.ToLowerInvariant());
    }
}
internal class DisplayNameFieldGenerator : StringFieldGenerator
{
    public DisplayNameFieldGenerator(StringDiversity diversity) : base("DisplayName", diversity)
    {
        Diversity = diversity ?? new StringDiversity {Type = DiversityType.Constant, Pattern = null};
    }
    protected override void StoreData(string value, ReplicationContext context)
    {
        context.NodeHeadData.DisplayName = value;
    }
}
internal class IndexFieldGenerator : IntFieldGenerator
{
    public IndexFieldGenerator(IntDiversity diversity) : base("Index", diversity)
    {
        Diversity = diversity ?? new IntDiversity { Type = DiversityType.Constant, MinValue = 0 };
    }
    public override void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        context.NodeHeadData.Index = value;
        context.SetIndexValue(IndexFieldName.Index, value);
    }
}
internal class OwnerIdFieldGenerator : IntFieldGenerator
{
    public OwnerIdFieldGenerator(IntDiversity diversity) : base("OwnerId", diversity)
    {
        Diversity = diversity ?? new IntDiversity { Type = DiversityType.Constant, MinValue = 1 }; // Admin Id
    }
    public override void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        context.NodeHeadData.OwnerId = value;
        context.SetIndexValue(IndexFieldName.OwnerId, value);
        context.SetIndexValue("Owner", new[] { value }); // From original index doc
    }
}
internal class VersionFieldGenerator : StringFieldGenerator
{
    public VersionFieldGenerator(StringDiversity diversity) : base ("Version", diversity)
    {
        Diversity = diversity ?? new StringDiversity { Type = DiversityType.Constant, Pattern = "v1.0.a" };
    }
    public override void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        var version = VersionNumber.Parse(value);
        context.VersionData.Version = version;
        if (value == null)
            return;
        var lowerValue = value.ToLowerInvariant();
        context.SetIndexValue(IndexFieldName.Version, lowerValue);
        context.SetIndexValue(IndexFieldName.IsMajor, version.IsMajor);
        context.SetIndexValue(IndexFieldName.IsPublic, version.Status == VersionStatus.Approved);
    }
}
internal class CreatedByIdFieldGenerator : IntFieldGenerator
{
    public CreatedByIdFieldGenerator(IntDiversity diversity) : base ("CreatedById", diversity)
    {
        Diversity = diversity ?? new IntDiversity { Type = DiversityType.Constant, MinValue = 1 }; // Admin Id
    }
    public override void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        context.NodeHeadData.CreatedById = value;
        context.VersionData.CreatedById = value;
        context.SetIndexValue(IndexFieldName.CreatedById, value);
        context.SetIndexValue("CreatedBy", new[] { value }); // From original index doc
    }
}
internal class ModifiedByIdFieldGenerator : IntFieldGenerator
{
    public ModifiedByIdFieldGenerator(IntDiversity diversity) : base ("ModifiedById", diversity)
    {
        Diversity = diversity ?? new IntDiversity { Type = DiversityType.Constant, MinValue = 1 }; // Admin Id
    }
    public override void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        context.NodeHeadData.ModifiedById = value;
        context.VersionData.ModifiedById = value;
        context.SetIndexValue(IndexFieldName.ModifiedById, value);
        context.SetIndexValue("ModifiedBy", new[] { value }); // From original index doc
    }
}
internal class CreationDateFieldGenerator : DateTimeFieldGenerator
{
    public CreationDateFieldGenerator(DateTimeDiversity diversity) : base("CreationDate", diversity) { }
    protected override void StoreData(DateTime value, ReplicationContext context)
    {
        context.NodeHeadData.CreationDate = value;
        context.VersionData.CreationDate = value;
    }
}
internal class ModificationDateFieldGenerator : DateTimeFieldGenerator
{
    public ModificationDateFieldGenerator(DateTimeDiversity diversity) : base("ModificationDate", diversity) { }
    protected override void StoreData(DateTime value, ReplicationContext context)
    {
        context.NodeHeadData.ModificationDate = value;
        context.VersionData.ModificationDate = value;
    }
}

/* ========================================================================= DYNAMIC FIELD GENERATORS */

internal class StringFieldGenerator : IFieldGenerator
{
    private readonly Random _rng = new Random();
    public string PropertyName { get; }
    public PropertyType PropertyType { get; }
    public StringDiversity Diversity { get; protected set; }
    public StringFieldGenerator(string propertyName, StringDiversity diversity)
    {
        PropertyName = propertyName;
        PropertyType = PropertyType.GetByName(propertyName);
        Diversity = diversity;
    }
    public virtual void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        StoreData(value, context);
        if (value == null)
            return;
        var lowerValue = value.ToLowerInvariant();
        context.SetIndexValue(PropertyName, lowerValue);
    }
    protected virtual void StoreData(string value, ReplicationContext context)
    {
        context.DynamicData.DynamicProperties[PropertyType] = value;
    }
    
    protected string Generate(ReplicationContext context, StringDiversity diversity)
    {
        var pattern = diversity.Pattern;

        Func<int, string> Replace = (i) => pattern.Replace("*", i.ToString(context.PaddingFormat));

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
                if (min >= max)
                    return Replace(min);
                return Replace(_rng.Next(min, max + 1));
            //return RandomNumberGenerator.GetInt32(min, max + 1);

            default: throw new ArgumentOutOfRangeException();
        }
    }
}
internal class TextFieldGenerator : StringFieldGenerator
{
    public TextFieldGenerator(string propertyName, StringDiversity diversity) : base(propertyName, diversity) { }
    protected override void StoreData(string value, ReplicationContext context)
    {
        context.DynamicData.LongTextProperties[PropertyType] = value;
    }
}
internal class IntFieldGenerator : IFieldGenerator
{
    private readonly Random _rng = new Random();
    public string PropertyName { get; }
    public PropertyType PropertyType { get; }
    public IntDiversity Diversity { get; protected set; }
    public IntFieldGenerator(string propertyName, IntDiversity diversity)
    {
        PropertyName = propertyName;
        PropertyType = PropertyType.GetByName(propertyName);
        Diversity = diversity;
    }
    public virtual void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        StoreData(value, context);
        context.SetIndexValue(PropertyName, value);
    }
    protected virtual void StoreData(int value, ReplicationContext context)
    {
        context.DynamicData.DynamicProperties[PropertyType] = value;
    }
    protected int Generate(ReplicationContext context, IntDiversity diversity)
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
}
internal class DateTimeFieldGenerator : IFieldGenerator
{
    private readonly Random _rng = new Random();
    public string PropertyName { get; }
    public PropertyType PropertyType { get; }
    public DateTimeDiversity Diversity { get; protected set; }
    public DateTimeFieldGenerator(string propertyName, DateTimeDiversity diversity)
    {
        PropertyName = propertyName;
        PropertyType = PropertyType.GetByName(propertyName);
        Diversity = diversity;
    }
    public virtual void Generate(ReplicationContext context)
    {
        var value = Generate(context, Diversity);
        StoreData(value, context);
        context.SetIndexValue(PropertyName, value);
    }
    protected virtual void StoreData(DateTime value, ReplicationContext context)
    {
        context.DynamicData.DynamicProperties[PropertyType] = value;
    }
    protected DateTime Generate(ReplicationContext context, DateTimeDiversity diversity)
    {
        if (diversity == null)
            return DateTime.UtcNow;

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

}

using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public interface IFieldGenerator
{
    string PropertyName { get; }
    PropertyType PropertyType { get; }
    IDiversity Diversity { get; }

    void Generate(ReplicationContext context);
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
    private bool _generationStarted;
    private int _currentSequenceValue;

    public string PropertyName { get; }
    public PropertyType PropertyType { get; }
    public StringDiversity Diversity { get; protected set; }
    IDiversity IFieldGenerator.Diversity => Diversity;
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
        var step = diversity.Sequence?.Step ?? 1;

        switch (diversity.Type)
        {
            case DiversityType.Constant:
                return pattern;

            case DiversityType.Sequence:
                if (max == min)
                    return Replace(min);
                if (max < min)
                    return Replace(context.CurrentCount + min);
                //var offset = context.CurrentCount % (max - min + 1);
                //return Replace(min + offset);
                if (!_generationStarted)
                {
                    _currentSequenceValue = min;
                    _generationStarted = true;
                }
                else
                {
                    var d = _currentSequenceValue + step;
                    if (d > max)
                        d = min;
                    _currentSequenceValue = d;
                }
                return Replace(_currentSequenceValue);

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
    private bool _generationStarted;

    public string PropertyName { get; }
    public PropertyType PropertyType { get; }
    public IntDiversity Diversity { get; protected set; }
    IDiversity IFieldGenerator.Diversity => Diversity;
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
        var step = diversity.Step;

        switch (diversity.Type)
        {
            case DiversityType.Constant:
                return min;

            case DiversityType.Sequence:
                if (max == min)
                    return min;
                if (max < min)
                    return context.CurrentCount + min;
                if (!_generationStarted)
                {
                    diversity.Current = min;
                    _generationStarted = true;
                }
                else
                {
                    var d = diversity.Current + step;
                    if (d > max)
                        d = min;
                    diversity.Current = d;
                }
                return diversity.Current;

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
    IDiversity IFieldGenerator.Diversity => Diversity;
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

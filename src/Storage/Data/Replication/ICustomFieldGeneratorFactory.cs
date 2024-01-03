using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public interface ICustomFieldGeneratorFactory
{
    ICustomDiversity CreateDiversity(string name);
    IFieldGenerator CreateFieldGenerator(string propertyName, ICustomDiversity diversity);
}

public class CustomFieldGeneratorFactory : ICustomFieldGeneratorFactory
{
    public ICustomDiversity CreateDiversity(string name)
    {
        return name.Equals("RandomReference", StringComparison.InvariantCultureIgnoreCase)
            ? new RandomReferenceDiversity()
            : null;
    }

    public IFieldGenerator CreateFieldGenerator(string propertyName, ICustomDiversity diversity)
    {
        return diversity is RandomReferenceDiversity
            ? new RandomReferenceFieldGenerator(propertyName, diversity)
            : null;
    }
}


public class RandomReferenceDiversity : ICustomDiversity
{
    public DiversityType Type
    {
        get => DiversityType.Custom;
        set => throw new NotImplementedException();
    }
    public DataType DataType => DataType.Reference;
    public object Current { get; set; }

    public int[] Array { get; private set; }

    public void Parse(string diversitySettings)
    {
        Array = JsonConvert.DeserializeObject<int[]>(diversitySettings);
    }
}
internal class RandomReferenceFieldGenerator : IFieldGenerator
{
    private readonly Random _rng = new Random();

    public string PropertyName { get; }
    public PropertyType PropertyType { get; }
    public RandomReferenceDiversity Diversity { get; }
    IDiversity IFieldGenerator.Diversity => Diversity;
    public RandomReferenceFieldGenerator(string propertyName, IDiversity diversity)
    {
        PropertyName = propertyName;
        PropertyType = PropertyType.GetByName(propertyName);
        Diversity = (RandomReferenceDiversity)diversity;
    }

    public void Generate(ReplicationContext context)
    {
        var length = Diversity.Array.Length;
        var index = length < 2 ? 0 : _rng.Next(0, length);
        var value = Diversity.Array[index];
        context.DynamicData.ReferenceProperties[PropertyType] = new List<int> { value };
        context.SetIndexValue(PropertyName, value);
    }
}

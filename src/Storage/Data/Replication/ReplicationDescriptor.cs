using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class ReplicationDescriptor
{
    public int MaxCount { get; set; }
    public int MaxItemsPerFolder { get; set; }
    public int MaxFoldersPerFolder { get; set; }
    public int FirstFolderIndex { get; set; }
    public Dictionary<string, string> Fields { get; set; }
    [JsonIgnore]
    public IDictionary<string, IDiversity> Diversity { get; set; }

    private char[] _whitespaces = " \t\r\n".ToCharArray();
    public void Initialize(NodeType nodeType)
    {
        if (MaxCount < 1)
            MaxCount = 10;
        if (MaxItemsPerFolder < 1)
            MaxItemsPerFolder = 100;
        if (MaxFoldersPerFolder < 1)
            MaxFoldersPerFolder = 100;

        var diversity = new Dictionary<string, IDiversity>();
        Diversity = diversity;
        if (Fields == null)
            return;

        var errors = new List<Exception>();
        foreach (var item in Fields)
        {
            var fieldName = item.Key;
            var dataType = GetDataType(fieldName);
            var diversitySource = item.Value;

            if (nodeType.PropertyTypes[fieldName] ==  null && !IsWellKnownProperty(fieldName))
            {
                errors.Add(new InvalidOperationException($"Type \"{nodeType.Name}\" does not have a field named \"{fieldName}\"."));
                continue;
            }
            if (dataType == null)
            {
                errors.Add(new InvalidOperationException($"Unknown field: '{fieldName}'."));
                continue;
            }

            var parser = new DiversityParser(fieldName, dataType.Value, diversitySource);
            try
            {
                diversity.Add(fieldName, parser.Parse());
            }
            catch (DiversityParserException e)
            {
                errors.Add(e);
            }
        }

        if(errors.Count > 0)
            throw new AggregateException(errors);
    }

    private bool IsWellKnownProperty(string fieldName)
    {
        return WellKnownProperties.ContainsKey(fieldName);
    }
    private DataType? GetDataType(string fieldName)
    {
        if (WellKnownProperties.TryGetValue(fieldName, out var dataType))
            return dataType;
        return PropertyType.GetByName(fieldName)?.DataType;
    }

    public static readonly IDictionary<string, DataType> WellKnownProperties =
        new ReadOnlyDictionary<string, DataType>(new Dictionary<string, DataType>
        {
            {"Name", DataType.String},
            {"DisplayName", DataType.String},
            {"Index", DataType.Int},
            {"OwnerId", DataType.Int},
            {"Version", DataType.String},
            {"CreatedById", DataType.DateTime},
            {"ModifiedById", DataType.DateTime},
            {"CreationDate", DataType.DateTime},
            {"ModificationDate", DataType.DateTime}
        });
}

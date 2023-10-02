using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class ReplicationDescriptor
{
    public int CountMax { get; set; }
    public int MaxItemsPerFolder { get; set; }
    public int MaxFoldersPerFolder { get; set; }
    public int FirstFolderIndex { get; set; }
    public Dictionary<string, string> Fields { get; set; }
    [JsonIgnore]
    public IDictionary<string, IDiversity> Diversity { get; set; }

    private char[] _whitespaces = " \t\r\n".ToCharArray();
    public void Initialize()
    {
        if (CountMax < 1)
            CountMax = 10;
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

    private DataType? GetDataType(string fieldName)
    {
        switch (fieldName)
        {
            case "Name": return DataType.String;
            case "DisplayName": return DataType.String;
            case "Index": return DataType.Int;
            case "OwnerId": return DataType.Int;
            case "Version": return DataType.String;
            case "CreatedById": return DataType.DateTime;
            case "ModifiedById": return DataType.DateTime;
            case "CreationDate": return DataType.DateTime;
            case "ModificationDate": return DataType.DateTime;
        }

        return PropertyType.GetByName(fieldName)?.DataType;
    }
}

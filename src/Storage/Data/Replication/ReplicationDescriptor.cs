using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class ReplicationDescriptor
{
    public int CountMax { get; set; }
    public int MaxItemsPerFolder { get; set; }
    public int MaxFoldersPerFolder { get; set; }
    public int FirstFolderIndex { get; set; }
    public Dictionary<string, string> DiversityControl { get; set; }
    [JsonIgnore]
    public IDictionary<string, IDiversity> Diversity { get; set; }

    public static ReplicationDescriptor Parse(object descriptorJson)
    {
        throw new NotImplementedException();
        /*
        if (descriptorJson == null)
            throw new ArgumentNullException(nameof(descriptor));
        if (descriptor.Length == 0)
            throw new ArgumentException($"The '{nameof(descriptor)}' argument cannot be empty.");

        ReplicationDescriptor deserialized = null;
        try
        {
            deserialized = JsonConvert.DeserializeObject<ReplicationDescriptor>(descriptor);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"The value of the '{nameof(descriptor)}' argument cannot be " +
                                        $"recognized as a valid ReplicationDescriptor.", e);
        }

        if (deserialized.MaxItemsPerFolder < 1)
            deserialized.MaxItemsPerFolder = 100;

        if (deserialized.MaxFoldersPerFolder < 1)
            deserialized.MaxFoldersPerFolder = 100;

        return deserialized;
        */
    }
}

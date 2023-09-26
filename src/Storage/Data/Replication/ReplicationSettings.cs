using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class ReplicationSettings
{
    public int CountMax { get; set; }
    public int MaxItemsPerFolder { get; set; }
    public int MaxFoldersPerFolder { get; set; }
    public int FirstFolderIndex { get; set; }
    public IDictionary<string, IDiversity> Diversity { get; set; }

    public static ReplicationSettings Parse(string descriptor)
    {
        throw new NotImplementedException();
    }
}


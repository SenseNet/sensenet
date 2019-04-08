using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class VersionData
    {
        public int VersionId;
        public int NodeId;
        public VersionNumber Version;
        public DateTime CreationDate;
        public int CreatedById;
        public DateTime ModificationDate;
        public int ModifiedById;
        public IEnumerable<ChangedData> ChangedData;
        public long Timestamp;
    }
}

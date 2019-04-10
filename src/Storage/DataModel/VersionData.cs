using System;
using System.Collections.Generic;
using System.Globalization;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class VersionData : IDataModel
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

        public void SetProperty(string name, string value)
        {
            switch (name)
            {
                case "VersionId":
                    VersionId = int.Parse(value);
                    break;
                case "NodeId":
                    NodeId = int.Parse(value);
                    break;
                case "Version":
                    Version = VersionNumber.Parse(value);
                    break;
                case "CreationDate":
                    CreationDate = DateTime.Parse(value);
                    break;
                case "CreatedById":
                    CreatedById = int.Parse(value);
                    break;
                case "ModificationDate":
                    ModificationDate = DateTime.Parse(value);
                    break;
                case "ModifiedById":
                    ModifiedById = int.Parse(value);
                    break;
                case "ChangedData":
                    throw new NotImplementedException();
                case "Timestamp":
                    Timestamp = long.Parse(value);
                    break;
                default:
                    throw new ApplicationException("Unknown property: " + name);
            }
        }
    }
}

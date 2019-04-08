using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class DynamicPropertyData
    {
        public int VersionId;
        public PropertyType[] PropertyTypes;
        public IDictionary<PropertyType, object> DynamicProperties;
        public IDictionary<PropertyType, BinaryDataValue> BinaryProperties;
        public IDictionary<PropertyType, string> VeryLongTextValues; //UNDONE:DB!!! Discuss: requires or not
    }
}

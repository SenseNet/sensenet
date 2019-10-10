using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class RepositorySchemaData
    {
        public long Timestamp;
        public List<PropertyTypeData> PropertyTypes;
        public List<NodeTypeData> NodeTypes;
        public List<ContentListTypeData> ContentListTypes;

        public RepositorySchemaData Clone()
        {
            return new RepositorySchemaData
            {
                Timestamp = Timestamp,
                PropertyTypes = PropertyTypes?.Select(x => x.Clone()).ToList() ?? new List<PropertyTypeData>(),
                NodeTypes = NodeTypes?.Select(x => x.Clone()).ToList() ?? new List<NodeTypeData>(),
                ContentListTypes = ContentListTypes?.Select(x => x.Clone()).ToList() ?? new List<ContentListTypeData>(),
            };
        }
    }
}

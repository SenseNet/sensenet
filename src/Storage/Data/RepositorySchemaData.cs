using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public interface ISchemaItemData
    {
        int Id { get; set; }
        string Name { get; set; }
    }

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

    [DebuggerDisplay("{Name}: {DataType}, {Mapping}")]
    public class PropertyTypeData : ISchemaItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DataType DataType { get; set; }
        public int Mapping { get; set; }
        public bool IsContentListProperty { get; set; }

        public PropertyTypeData Clone()
        {
            return new PropertyTypeData
            {
                Id = Id,
                Name = Name,
                DataType = DataType,
                Mapping = Mapping,
                IsContentListProperty = IsContentListProperty
            };
        }
    }

    [DebuggerDisplay("{Name}: {ParentName}")]
    public class NodeTypeData : ISchemaItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string ClassName { get; set; }
        public List<string> Properties { get; set; }

        public NodeTypeData Clone()
        {
            return new NodeTypeData
            {
                Id = Id,
                Name = Name,
                ParentName = ParentName,
                ClassName = ClassName,
                Properties = Properties.ToList()
            };
        }
    }

    [DebuggerDisplay("{Name}")]
    public class ContentListTypeData : ISchemaItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Properties { get; set; }

        public ContentListTypeData Clone()
        {
            return new ContentListTypeData
            {
                Id = Id,
                Name = Name,
                Properties = Properties.ToList()
            };
        }
    }
}

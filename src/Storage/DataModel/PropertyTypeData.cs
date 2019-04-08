using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
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
}

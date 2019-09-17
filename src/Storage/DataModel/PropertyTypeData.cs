using System;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    [DebuggerDisplay("{Name}: {DataType}, {Mapping}")]
    public class PropertyTypeData : ISchemaItemData, IDataModel
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

        public void SetProperty(string name, string value)
        {
            switch (name)
            {
                case "Id":
                    Id = int.Parse(value);
                    break;
                case "Name":
                    Name = value;
                    break;
                case "DataType":
                    DataType = (DataType)Enum.Parse(typeof(DataType), value, true);
                    break;
                case "Mapping":
                    Mapping = int.Parse(value);
                    break;
                case "IsContentListProperty":
                    IsContentListProperty = value.ToLowerInvariant() == "true";
                    break;
                default:
                    throw new ApplicationException("Unknown property: " + name);
            }
        }
    }
}

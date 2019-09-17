using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    [DebuggerDisplay("{Name}: {ParentName}")]
    public class NodeTypeData : ISchemaItemData, IDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string ClassName { get; set; }
        public List<string> Properties { get; set; } = new List<string>();

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
                case "ParentName":
                    ParentName = value;
                    break;
                case "ClassName":
                    ClassName = value;
                    break;
                case "Properties":
                    var names = value.Length > 2
                        ? value.Substring(1, value.Length - 2).Split(' ').ToList()
                        : new List<string>();
                    Properties = names;
                    break;
                default:
                    throw new ApplicationException("Unknown property: " + name);
            }
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
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
}

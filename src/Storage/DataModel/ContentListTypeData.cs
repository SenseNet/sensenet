using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    [DebuggerDisplay("{Name}")]
    public class ContentListTypeData : ISchemaItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Properties { get; set; } = new List<string>();

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

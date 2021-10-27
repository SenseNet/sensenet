using System.Collections.Generic;

namespace SenseNet.OData.IO
{
    internal class EntryModel
    {
        public string Identity { get; set; }
        public bool LocalOnly { get; set; }
        public Dictionary<string, object> Permissions { get; set; }
    }
    internal class PermissionModel
    {
        public bool IsInherited { get; set; }
        public EntryModel[] Entries { get; set; }
    }
}

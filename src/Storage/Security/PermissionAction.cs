using System.Collections.Generic;
using SenseNet.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Security
{
    internal class PermissionAction
    {
        public int EntityId { get; set; }
        public bool Break { get; set; }
        public bool Unbreak { get; set; }
        public List<StoredAce> Entries { get; set; }
    }
}

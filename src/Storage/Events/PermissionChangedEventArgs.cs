using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Events
{
    public class PermissionChangedEventArgs : NodeEventArgs
    {
        public NodeHead[] RelatedNodeHeads { get; private set; }

        internal PermissionChangedEventArgs(Node node, IDictionary<string, object> customData, IEnumerable<ChangedData> changedData)
            : base(node, NodeEvent.PermissionChanged, customData, null, changedData)
        {
            RelatedNodeHeads = node == null
                ? new NodeHead[0]
                : new[] {NodeHead.Get(node.Id)};
        }

        internal PermissionChangedEventArgs(NodeHead[] relatedNodeHeads, IDictionary<string, object> customData, IEnumerable<ChangedData> changedData)
            : base(null, NodeEvent.PermissionChanged, customData, null, changedData)
        {
            RelatedNodeHeads = relatedNodeHeads ?? new NodeHead[0];
        }
    }
}

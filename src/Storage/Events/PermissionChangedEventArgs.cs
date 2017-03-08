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

        public PermissionChangedEventArgs(Node node, object customData, IEnumerable<ChangedData> changedData)
            : base(node, NodeEvent.PermissionChanged, customData, null, changedData)
        {
            RelatedNodeHeads = node == null
                ? new NodeHead[0]
                : new[] {NodeHead.Get(node.Id)};
        }

        public PermissionChangedEventArgs(NodeHead[] relatedNodeHeads, object customData, IEnumerable<ChangedData> changedData)
            : base(null, NodeEvent.PermissionChanged, customData, null, changedData)
        {
            RelatedNodeHeads = relatedNodeHeads ?? new NodeHead[0];
        }
    }
}

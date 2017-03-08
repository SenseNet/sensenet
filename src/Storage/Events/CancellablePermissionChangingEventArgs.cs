using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Events
{
    public class CancellablePermissionChangingEventArgs : CancellableNodeEventArgs
    {
        public NodeHead[] RelatedNodeHeads { get; private set; }

        public CancellablePermissionChangingEventArgs(Node node, IEnumerable<ChangedData> changedData)
            : base(node, CancellableNodeEvent.PermissionChanging, changedData)
        {
            RelatedNodeHeads = node == null
                ? new NodeHead[0]
                : new[] {NodeHead.Get(node.Id)};
        }

        public CancellablePermissionChangingEventArgs(NodeHead[] relatedNodeHeads, IEnumerable<ChangedData> changedData)
            : base(null, CancellableNodeEvent.PermissionChanging, changedData)
        {
            RelatedNodeHeads = relatedNodeHeads ?? new NodeHead[0];
        }
    }
}

using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ApplicationModel
{
    internal sealed class AppStorageInvalidator : NodeObserver
    {
        protected override void OnNodeCopied(object sender, NodeOperationEventArgs e)
        {
            bool invalidated = OnInvalidate(sender, e.SourceNode);

            if (!invalidated)
                OnInvalidate(sender, e.TargetNode);
        }
        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            OnInvalidate(sender, e.SourceNode);
        }
        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            var invalidated = false;

            if (e.OriginalSourcePath.CompareTo(e.SourceNode.Path) != 0)
                invalidated = OnInvalidate(sender, e.OriginalSourcePath);

            if (!invalidated)
                OnInvalidate(sender, e.SourceNode);
        }
        protected override void OnNodeDeleted(object sender, NodeEventArgs e)
        {
            OnInvalidate(sender, e.SourceNode);
        }
        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            OnInvalidate(sender, e.SourceNode);
        }
        protected override void OnNodeMoved(object sender, NodeOperationEventArgs e)
        {
            var invalidated = OnInvalidate(sender, e.OriginalSourcePath);

            if (!invalidated)
                OnInvalidate(sender, e.TargetNode);
        }

        public static bool OnInvalidate(object sender, Node node)
        {
            return ApplicationStorage.InvalidateByNode(node);
        }

        public static bool OnInvalidate(object sender, string path)
        {
            return ApplicationStorage.InvalidateByPath(path);
        }
    }
}

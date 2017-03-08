using System;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;

namespace SenseNet.Portal
{
    internal class SkinObserver : NodeObserver
    {
        public static readonly string SkinStartPath = string.Concat(RepositoryStructure.SkinRootFolderPath, RepositoryPath.PathSeparator);

        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            base.OnNodeCreated(sender, e);

            if (e.SourceNode.Path.StartsWith(SkinObserver.SkinStartPath))
                SkinManagerBase.Instance.AddToMap(e.SourceNode.Path);
        }
        protected override void OnNodeDeleted(object sender, NodeEventArgs e)
        {
            base.OnNodeDeleted(sender, e);

            if (e.SourceNode.Path.StartsWith(SkinObserver.SkinStartPath))
                SkinManagerBase.Instance.RemoveFromMap(e.SourceNode.Path);
        }
        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            base.OnNodeDeletedPhysically(sender, e);

            if (e.SourceNode.Path.StartsWith(SkinObserver.SkinStartPath))
                SkinManagerBase.Instance.RemoveFromMap(e.SourceNode.Path);
        }
        protected override void OnNodeCopied(object sender, NodeOperationEventArgs e)
        {
            base.OnNodeCopied(sender, e);

            if (e.TargetNode.Path.StartsWith(SkinObserver.SkinStartPath))
            {
                var targetPath = RepositoryPath.Combine(e.TargetNode.Path, e.SourceNode.Name);
                SkinManagerBase.Instance.AddToMap(targetPath);
            }
        }
        protected override void OnNodeMoved(object sender, NodeOperationEventArgs e)
        {
            base.OnNodeMoved(sender, e);

            if (e.OriginalSourcePath.StartsWith(SkinObserver.SkinStartPath))
                SkinManagerBase.Instance.RemoveFromMap(e.OriginalSourcePath);
            if (e.TargetNode.Path.StartsWith(SkinObserver.SkinStartPath))
            {
                var targetPath = RepositoryPath.Combine(e.TargetNode.Path, e.SourceNode.Name);
                SkinManagerBase.Instance.AddToMap(targetPath);
            }
        }
        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            base.OnNodeModified(sender, e);

            // renamed?
            if (!string.Equals(e.OriginalSourcePath, e.SourceNode.Path, StringComparison.InvariantCulture))
            {
                if (e.OriginalSourcePath.StartsWith(SkinObserver.SkinStartPath))
                    SkinManagerBase.Instance.RemoveFromMap(e.OriginalSourcePath);
                if (e.SourceNode.Path.StartsWith(SkinObserver.SkinStartPath))
                    SkinManagerBase.Instance.AddToMap(e.SourceNode.Path);
            }
        }
    }
}

using SenseNet.ContentRepository.Storage.Events;

namespace SenseNet.ContentRepository.Sharing
{
    internal class SharingNodeObserver : NodeObserver
    {
        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            base.OnNodeDeletedPhysically(sender, e);

            SharingHandler.OnContentDeleted(e?.SourceNode);
        }
    }
}

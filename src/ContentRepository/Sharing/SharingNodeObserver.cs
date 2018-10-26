using System.Linq;
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

        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            base.OnNodeCreated(sender, e);

            if (e?.SourceNode is User user)
            {
                SharingHandler.OnUserCreated(user);
            }
        }

        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            base.OnNodeModified(sender, e);

            if (!(e.SourceNode is User user))
                return;

            var emailChange = e.ChangedData.FirstOrDefault(cd => cd.Name == "Email");
            if (emailChange == null)
                return;
            
            SharingHandler.OnUserChanged(user, (string)emailChange.Original);
        }
    }
}

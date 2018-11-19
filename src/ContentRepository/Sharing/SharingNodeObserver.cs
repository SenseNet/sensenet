using System.Linq;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Notifies the sharing system about relevant changes in the repository. For example
    /// when a subtree, a user or a group is deleted, or a user is created or changed.
    /// </summary>
    internal class SharingNodeObserver : NodeObserver
    {
        private const string SharingGroupIdsCustomDataKey = "SnSharingGroupIds";

        protected override void OnNodeDeletingPhysically(object sender, CancellableNodeEventArgs e)
        {
            base.OnNodeDeletingPhysically(sender, e);

            // Collect sharing group ids for publicly shared content 
            // in the subtree to delete them later in the background.
            var sharingGroupIds = SharingHandler.GetSharingGroupIdsForSubtree(e.SourceNode);
            if (sharingGroupIds?.Any() ?? false)
                e.SetCustomData(SharingGroupIdsCustomDataKey, sharingGroupIds);
        }

        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            base.OnNodeDeletedPhysically(sender, e);

            SharingHandler.OnContentDeleted(e?.SourceNode);

            // if there are sharing groups that should be deleted
            var sharingGroupIds = e?.GetCustomData(SharingGroupIdsCustomDataKey) as int[];
            if (sharingGroupIds?.Any() ?? false)
            {
                //TODO: if the list is too big, postpone deleting the groups
                // using a local task management API.

                System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            using (new SystemAccount())
                            {
                                Parallel.ForEach(sharingGroupIds.Select(Node.LoadNode).Where(n => n != null),
                                    new ParallelOptions { MaxDegreeOfParallelism = 4 },
                                    group => group.ForceDelete());
                            }
                        }
                        catch (System.Exception ex)
                        {
                            SnLog.WriteException(ex, "Error during deleting sharing groups.");
                        }
                    });
            }
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

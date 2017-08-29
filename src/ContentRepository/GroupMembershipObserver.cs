using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Search;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class GroupMembershipObserver : NodeObserver
    {
        protected override void OnNodeMoved(object sender, NodeOperationEventArgs e)
        {
            // We do not have to deal with content outside of the IMS folder, because
            // moving local groups does not involve any membership change.
            if (!e.OriginalSourcePath.StartsWith(RepositoryStructure.ImsFolderPath + RepositoryPath.PathSeparator) && !e.SourceNode.Path.StartsWith(RepositoryStructure.ImsFolderPath + RepositoryPath.PathSeparator))
                return;

            base.OnNodeMoved(sender, e);

            var movedUsers = new List<int>();
            var movedGroups = new List<int>();

            // if the moved content is an identity, put it into the appropriate list
            if (e.SourceNode is User)
                movedUsers.Add(e.SourceNode.Id);
            else if (e.SourceNode is Group || e.SourceNode is OrganizationalUnit)
                movedGroups.Add(e.SourceNode.Id);
            else
            {
                // If the moved content is an irrelevant container (e.g. a folder), collect relevant (first-level) child content (users, groups 
                // and child orgunits even inside simple subfolders). These are already moved to the new location, but we only need their ids.
                using (new SystemAccount())
                {
                    CollectSecurityIdentityChildren(NodeHead.Get(e.SourceNode.Path), movedUsers, movedGroups);
                }
            }

            // empty collections: nothing to do
            if (movedUsers.Count == 0 && movedGroups.Count == 0)
                return;

            // find the original parent orgunit (if there was one)
            var parent = Node.LoadNode(RepositoryPath.GetParentPath(e.OriginalSourcePath));
            var originalParentId = 0;
            var targetParentId = 0;
            if (parent is OrganizationalUnit)
            {
                originalParentId = parent.Id;
            }
            else
            {
                using (new SystemAccount())
                {
                    parent = GetFirstOrgUnitParent(parent);
                    if (parent != null)
                        originalParentId = parent.Id;
                }
            }

            // find the target parent orgunit (if there is one)
            using (new SystemAccount())
            {
                parent = GetFirstOrgUnitParent(e.SourceNode);
                if (parent != null)
                    targetParentId = parent.Id;
            }

            // remove relevant child content from the original parent org unit (if it is different from the target)
            if (originalParentId > 0 && originalParentId != targetParentId)
                SecurityHandler.RemoveMembers(originalParentId, movedUsers, movedGroups);

            // add the previously collected identities to the target orgunit (if it is different from the original)
            if (targetParentId > 0 && originalParentId != targetParentId)
                SecurityHandler.AddMembers(targetParentId, movedUsers, movedGroups);
        }

        protected override void OnNodeDeletingPhysically(object sender, CancellableNodeEventArgs e)
        {
            base.OnNodeDeletingPhysically(sender, e);

            // Memorize user, orgunit and group ids that should be removed from the security 
            // component after the delete operation succeeded.
            e.CustomData = GetSecurityIdentityIds(e.SourceNode);
        }

        protected override void OnNodeDeletedPhysically(object sender, NodeEventArgs e)
        {
            base.OnNodeDeletedPhysically(sender, e);

            var ids = e.CustomData as List<int>;
            if (ids != null)
                SecurityHandler.DeleteIdentities(ids);
        }

        // ======================================================================================= Helper methods

        /// <summary>
        /// Start a parent walk and get the first parent that is an organizational unit.
        /// </summary>
        internal static OrganizationalUnit GetFirstOrgUnitParent(Node node)
        {
            if (node == null)
                return null;

            if (!node.Path.StartsWith(RepositoryStructure.ImsFolderPath + RepositoryPath.PathSeparator))
                return null;

            var parent = node.Parent;
            if (parent == null)
                return null;

            while ((parent != null) && !(parent is OrganizationalUnit) && !parent.Path.Equals(RepositoryStructure.ImsFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                parent = parent.Parent;
            }

            return parent as OrganizationalUnit;
        }

        /// <summary>
        /// Gets all security identities (users, groups and org units) in a subtree.
        /// </summary>
        private static List<int> GetSecurityIdentityIds(Node node)
        {
            using (new SystemAccount())
            {
                // import scenario
                if (!StorageContext.Search.ContentQueryIsAllowed)
                {
                    var resultIds = NodeQuery.QueryNodesByTypeAndPathAndName(new[]
                    {
                        ActiveSchema.NodeTypes["Group"], 
                        ActiveSchema.NodeTypes["OrganizationalUnit"],
                        ActiveSchema.NodeTypes["User"]
                    },
                    false, node.Path, true, null).Identifiers.ToList();

                    // workaround for the nodequery above: it does not return the root node itself
                    if (node is ISecurityMember && !resultIds.Contains(node.Id))
                        resultIds.Add(node.Id);

                    return resultIds;
                }

                return ContentQuery_NEW.Query(SafeQueries.SecurityIdentitiesInTree, QuerySettings.AdminSettings, node.Path).Identifiers.ToList();
            }
        }

        /// <summary>
        /// Collects first-level child content that are identity types (users, groups or orgunits). If it encounters 
        /// folders (or other non-identity containers) it will dive deeper for identities.
        /// </summary>
        private static void CollectSecurityIdentityChildren(NodeHead head, ICollection<int> userIds, ICollection<int> groupIds)
        {
            foreach (var childHead in ContentQuery_NEW.Query(SafeQueries.InFolder, QuerySettings.AdminSettings, head.Path).Identifiers.Select(NodeHead.Get).Where(h => h != null))
            {
                // in case of identity types: simply add them to the appropriate collection and move on
                if (childHead.GetNodeType().IsInstaceOfOrDerivedFrom("User"))
                {
                    if (!userIds.Contains(childHead.Id))
                        userIds.Add(childHead.Id);
                }
                else if (childHead.GetNodeType().IsInstaceOfOrDerivedFrom("Group") ||
                    childHead.GetNodeType().IsInstaceOfOrDerivedFrom("OrganizationalUnit"))
                {
                    if (!groupIds.Contains(childHead.Id))
                        groupIds.Add(childHead.Id);
                }
                else 
                {
                    // collect identities recursively inside a folder
                    CollectSecurityIdentityChildren(childHead, userIds, groupIds);
                }
            }
        }
    }
}

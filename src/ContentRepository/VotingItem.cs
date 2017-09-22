using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class VotingItem : GenericContent
    {
        public VotingItem(Node parent) : this(parent, null)
        {
        }
        public VotingItem(Node parent, string nodeTypeName) : base(parent, nodeTypeName)
        {
        }
        protected VotingItem(NodeToken nt) : base(nt)
        {    
        }

        protected override void OnCreating(object sender, SenseNet.ContentRepository.Storage.Events.CancellableNodeEventArgs e)
        {
            base.OnCreating(sender, e);

            if (!StorageContext.Search.ContentQueryIsAllowed)
                return;

            var parent = e.SourceNode.Parent;
            var searchPath = parent is Voting ? parent.Path : parent.ParentPath;

            // Count Voting Items
            var votingItemCount = ContentQuery.Query(SafeQueries.InTreeAndTypeIsCountOnly,
                new QuerySettings { EnableAutofilters = FilterStatus.Disabled },
                searchPath, typeof(VotingItem).Name).Count;

            // Get children (VotingItems) count
            String tempName;
            if (votingItemCount < 10 && votingItemCount != 9)
                tempName = "VotingItem_0" + (votingItemCount + 1);
            else
                tempName = "VotingItem_" + (votingItemCount + 1);

            // If node already exits
            while (Node.Exists(RepositoryPath.Combine(parent.Path, tempName)))
            {
                votingItemCount++;
                if (votingItemCount < 10)
                    tempName = "VotingItem_0" + (votingItemCount + 1);
                else
                    tempName = "VotingItem_" + (votingItemCount + 1);
            }

            e.SourceNode["DisplayName"] = tempName;
            e.SourceNode["Name"] = tempName.ToLower();
        }
    
    }
}

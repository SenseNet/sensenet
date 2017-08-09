using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Index;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Storage.Search;
using System.Threading;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal class AddTreeActivity : LuceneTreeActivity
    {
        private Document[] Documents { get; set; }

        private IEnumerable<Node> GetVersions(Node node)
        {
            var versionNumbers = Node.GetVersionNumbers(node.Id);
            var versions = from versionNumber in versionNumbers select Node.LoadNode(node.Id, versionNumber);
            var versionsArray = versions.ToArray();
            return versionsArray;
        }

        protected override bool ProtectedExecute()
        {
            return IndexManager.AddTree(TreeRoot, this.MoveOrRename ?? false, this.Id, this.IsUnprocessedActivity);
        }
    }
}

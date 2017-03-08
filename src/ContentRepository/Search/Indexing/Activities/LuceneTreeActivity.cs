using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using SenseNet.Search.Indexing;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal abstract class LuceneTreeActivity : LuceneIndexingActivity
    {
        public string TreeRoot
        {
            get
            {
                return this.Path;
            }
        }

        public override string ToString()
        {
            return String.Format("{0}: [{1}/{2}], {3}", this.GetType().Name, this.NodeId, this.VersionId, this.Path);
        }

        protected override string GetExtension()
        {
            return null;
        }
        protected override void SetExtension(string value)
        {
            // do nothing
        }
    }


}
using System;

namespace SenseNet.Search.Indexing.Activities
{
    [Serializable]
    internal abstract class TreeIndexingActivity : IndexingActivityBase
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
using System;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal abstract class TreeIndexingActivity : IndexingActivityBase
    {
        public string TreeRoot => this.Path;

        public override string ToString()
        {
            return $"{this.GetType().Name}: [{this.NodeId}/{this.VersionId}], {this.Path}";
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
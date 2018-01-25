using System;

namespace SenseNet.ContentRepository.Search.Indexing.Activities
{
    [Serializable]
    internal abstract class TreeIndexingActivity : IndexingActivityBase
    {
        public string TreeRoot => Path;

        public override string ToString()
        {
            return $"{GetType().Name}: [{NodeId}/{VersionId}], {Path}";
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
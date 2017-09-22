using SenseNet.ContentRepository.Storage;
using SenseNet.Search.Indexing.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search.Indexing
{
    public class IndexingActivityFactory : IIndexingActivityFactory
    {
        public static IndexingActivityFactory Instance = new IndexingActivityFactory();

        public IIndexingActivity CreateActivity(IndexingActivityType activityType)
        {
            IIndexingActivity activity = null;
            switch (activityType)
            {
                case IndexingActivityType.AddDocument: activity = new AddDocumentActivity(); break;
                case IndexingActivityType.AddTree: activity = new AddTreeActivity(); break;
                case IndexingActivityType.UpdateDocument: activity = new UpdateDocumentActivity(); break;
                case IndexingActivityType.RemoveTree: activity = new RemoveTreeActivity(); break;
                case IndexingActivityType.RemoveDocument: activity = new RemoveDocumentActivity(); break; //UNDONE:!!!!!!!! RemoveDocumentActivity: Unused case label
                case IndexingActivityType.Rebuild: activity = new RebuildActivity(); break;
                default: throw new NotSupportedException("Unknown IndexingActivityType: " + activityType);
            }
            activity.ActivityType = activityType;
            return activity;
        }
    }
}

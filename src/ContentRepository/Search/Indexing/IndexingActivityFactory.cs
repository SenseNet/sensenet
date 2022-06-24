﻿using System;
using SenseNet.ContentRepository.Search.Indexing.Activities;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Provides an interchangeable <see cref="IIndexingActivityFactory"/> implementation.
    /// </summary>
    public class IndexingActivityFactory : IIndexingActivityFactory
    {
        /// <inheritdoc />
        public IIndexingActivity CreateActivity(IndexingActivityType activityType)
        {
            IIndexingActivity activity;
            switch (activityType)
            {
                case IndexingActivityType.AddDocument: activity = new AddDocumentActivity(); break;
                case IndexingActivityType.AddTree: activity = new AddTreeActivity(); break;
                case IndexingActivityType.UpdateDocument: activity = new UpdateDocumentActivity(); break;
                case IndexingActivityType.RemoveTree: activity = new RemoveTreeActivity(); break;
                case IndexingActivityType.Rebuild: activity = new RebuildActivity(); break;
                case IndexingActivityType.Restore: activity = new RestoreActivity(); break;
                default: throw new NotSupportedException("Unknown IndexingActivityType: " + activityType);
            }
            activity.ActivityType = activityType;
            return activity;
        }
    }
}

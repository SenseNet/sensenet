﻿using SenseNet.ContentRepository.Storage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage
{
    public enum IndexingActivityType
    {
        AddDocument = 1,
        AddTree = 2,
        UpdateDocument = 3,
        RemoveTree = 4,
        Rebuild
    }

    public enum IndexingActivityState
    {
        Waiting,
        Running,
        Done
    }

    public interface IIndexingActivityFactory
    {
        IIndexingActivity CreateActivity(IndexingActivityType activityType);
    }

    public interface IIndexingActivity
    {
        int Id { get; set; }
        IndexingActivityType ActivityType { get; set; }
        DateTime CreationDate { get; set; }
        IndexingActivityState ActivityState { get; set; }
        DateTime? StartDate { get; set; }
        int NodeId { get; set; }
        int VersionId { get; set; }
        string Path { get; set; }
        long? VersionTimestamp { get; set; }
        IndexDocumentData IndexDocumentData { get; set; }
        bool FromDatabase { get; set; }
        bool IsUnprocessedActivity { get; set; }
        string Extension { get; set; }
    }
}

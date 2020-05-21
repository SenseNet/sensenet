using System;
using SenseNet.ContentRepository.Storage.Data;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines a type constant of the indexing activity.
    /// </summary>
    public enum IndexingActivityType
    {
        /// <summary>
        /// Add brand new document. Value = 1.
        /// </summary>
        AddDocument = 1,
        /// <summary>
        /// Add brand new document set. Value = 2.
        /// </summary>
        AddTree = 2,
        /// <summary>
        /// Update the document. Value = 3.
        /// </summary>
        UpdateDocument = 3,
        /// <summary>
        /// Remove the tree. Value = 4.
        /// </summary>
        RemoveTree = 4,
        /// <summary>
        /// Rebuild the index. Value = 5.
        /// </summary>
        Rebuild,
        /// <summary>
        /// Indicates that the index was restored. Value = 6.
        /// </summary>
        Restore
    }

    /// <summary>
    /// Defines a constats for persist the state of the indexing activity execution.
    /// </summary>
    public enum IndexingActivityRunningState
    {
        /// <summary>
        /// Wait for execution.
        /// </summary>
        Waiting,
        /// <summary>
        /// It is being executed right now.
        /// </summary>
        Running,
        /// <summary>
        /// Executed.
        /// </summary>
        Done
    }

    /// <summary>
    /// Defines an indexing activity for storage layer.
    /// </summary>
    public interface IIndexingActivity
    {
        /// <summary>
        /// Gets or sets the database id of the indexing activity.
        /// </summary>
        int Id { get; set; }
        /// <summary>
        /// Gets or sets the type constanct of the indexing activity.
        /// </summary>
        IndexingActivityType ActivityType { get; set; }
        /// <summary>
        /// Gets or sets the timestamp of the indexing activity's registration.
        /// </summary>
        DateTime CreationDate { get; set; }
        /// <summary>
        /// Gets or sets the running state constant of the indexing activity.
        /// </summary>
        IndexingActivityRunningState RunningState { get; set; }
        /// <summary>
        /// Gets or sets tye timestamp of the locking or lovk refreshing.
        /// This time is the base data for determining the running timeout.
        /// </summary>
        DateTime? LockTime { get; set; }
        /// <summary>
        /// Gets or sets the related node id or 0.
        /// </summary>
        int NodeId { get; set; }
        /// <summary>
        /// Gets or sets the related version id or 0.
        /// </summary>
        int VersionId { get; set; }
        /// <summary>
        /// Gets or sets the related content path.
        /// </summary>
        string Path { get; set; }
        /// <summary>
        /// Gets or sets the database timestamp of the related version or 0l.
        /// </summary>
        long? VersionTimestamp { get; set; }
        /// <summary>
        /// Gets or sets the stored data of the index document.
        /// </summary>
        IndexDocumentData IndexDocumentData { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if this instance is loaded from the database.
        /// </summary>
        bool FromDatabase { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if this instance is a part of the missing activity set.
        /// This case can occur in the system start sequence or a supplementer process triggered by the index health checker.
        /// </summary>
        bool IsUnprocessedActivity { get; set; }

        /// <summary>
        /// Gets and sets freeform additional indexing metadata.
        /// </summary>
        string Extension { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Search.Indexing.Activities;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal static class DistributedIndexingActivityQueue
    {
        internal static int IndexingActivityLoadingBufferSize = 200;
        internal static int IndexingOverloadWaitingTime = 100;

        internal static void HealthCheck()
        {
            if (IsWorking())
            {
                SnTrace.Index.Write("IAQ: Health check triggered but ignored.");
                return;
            }

            SnTrace.Index.Write("IAQ: Health check triggered.");

            var state = TerminationHistory.GetCurrentState();
            var gapsLength = state.Gaps.Length;
            if (gapsLength > 0)
            {
                SnTrace.IndexQueue.Write("IAQ: Health checker is processing {0} gap{1}.", gapsLength, gapsLength > 1 ? "s" : "");

                var notLoadedIds = state.Gaps.ToList();
                foreach (var indexingActivity in new IndexingActivityLoader(state.Gaps, false))
                {
                    var activity = (IndexingActivityBase) indexingActivity;
                    WaitIfOverloaded();
                    ExecuteActivity(activity);
                    notLoadedIds.Remove(activity.Id);
                }

                if (notLoadedIds.Count > 0)
                {
                    TerminationHistory.RemoveGaps(notLoadedIds);
                    SnTrace.IndexQueue.Write("IAQ: Health checker ignores the following activity ids after processing the gaps: {0}", notLoadedIds);
                }
            }

            var lastId = TerminationHistory.GetLastTerminatedId();
            var lastDbId = IndexManager.GetLastStoredIndexingActivityId();

            if (lastId < lastDbId)
            {
                SnTrace.IndexQueue.Write("IAQ: Health checker is processing activities from {0} to {1}", (lastId + 1), lastDbId);

                foreach (var indexingActivity in new IndexingActivityLoader(lastId + 1, lastDbId, false))
                {
                    var activity = (IndexingActivityBase) indexingActivity;
                    WaitIfOverloaded();
                    ExecuteActivity(activity);
                }
            }
        }

        private static void WaitIfOverloaded(bool startProcessing = false)
        {
            // We prevent memory overflow by limiting the number of activities that we
            // keep in memory. This method waits for the queue to be able to process
            // new activities.
            var logCount = 1;
            while (IsOverloaded())
            {
                // In case of startup, we have to start processing activities that are
                // already in the queue so that new ones can be added later.
                if (startProcessing)
                    DependencyManager.ActivityEnqueued();

                if (logCount++%10 == 1)
                    SnTrace.Index.Write("IAQ OVERLOAD waiting {0} milliseconds.", IndexingOverloadWaitingTime*10);
                
                Thread.Sleep(IndexingOverloadWaitingTime);
            }
        }

        public static bool IsWorking()
        {
            return !(Serializer.IsEmpty && DependencyManager.IsEmpty);
        }

        public static bool IsOverloaded()
        {
            // The indexing queue is overloaded if either the arrival queue
            // or the waiting set contains too many elements. In the future
            // this algorithm may be changed to a different one based on
            // activity size instead of count.
            return DependencyManager.WaitingSetLength >= Configuration.Indexing.IndexingActivityQueueMaxLength ||
                Serializer.QueueLength >= Configuration.Indexing.IndexingActivityQueueMaxLength;
        }

        public static void Startup(System.IO.TextWriter consoleOut)
        {
            // initalize from index
            var cud = IndexManager.IndexingEngine.ReadActivityStatusFromIndex();

            var gapsLength = cud.Gaps?.Length ?? 0;

            var lastDatabaseId = IndexManager.GetLastStoredIndexingActivityId();

            using (var op = SnTrace.Index.StartOperation("IAQ: InitializeFromIndex. LastIndexedActivityId: {0}, LastDatabaseId: {1}, TotalUnprocessed: {2}"
                , cud.LastActivityId, lastDatabaseId, lastDatabaseId - cud.LastActivityId + gapsLength))
            {
                Startup(lastDatabaseId, cud.LastActivityId, cud.Gaps, consoleOut);

                op.Successful = true;
            }

            IndexHealthMonitor.Start(consoleOut);
        }
        private static void Startup(int lastDatabaseId, int lastExecutedId, int[] gaps, System.IO.TextWriter consoleOut)
        {
            Serializer.Reset();
            DependencyManager.Reset();
            TerminationHistory.Reset(lastExecutedId, gaps);
            Serializer.Start(lastDatabaseId, lastExecutedId, gaps, consoleOut);
            IndexingActivityHistory.Reset();
        }

        public static void ShutDown()
        {
            SnTrace.IndexQueue.Write("Shutting down IndexingActivityQueue.");
            Serializer.ShutDown();
            IndexHealthMonitor.ShutDown();
        }

        public static IndexingActivityStatus GetCurrentCompletionState()
        {
            return TerminationHistory.GetCurrentState();
        }
        public static IndexingActivityQueueState GetCurrentState()
        {
            return new IndexingActivityQueueState
            {
                Serializer = Serializer.GetCurrentState(),
                DependencyManager = DependencyManager.GetCurrentState(),
                Termination = TerminationHistory.GetCurrentState()
            };
        }

        public static void ExecuteActivity(IndexingActivityBase activity)
        {
            Serializer.EnqueueActivity(activity);
        }

        /// <summary>Only for tests</summary>
        internal static void _setCurrentExecutionState(IndexingActivityStatus state)
        {
            Serializer.Reset(state.LastActivityId);
            DependencyManager.Reset();
            TerminationHistory.Reset(state.LastActivityId, state.Gaps);
        }

        private static class Serializer
        {
            internal static void Reset(int lastQueued = 0)
            {
                lock (ArrivalQueueLock)
                {

                    if (ArrivalQueue.Count > 0)
                        SnTrace.IndexQueue.Write("IAQ: RESET: ArrivalQueue.Count: {0}", ArrivalQueue.Count);
                    
                    foreach (var activity in ArrivalQueue)
                        activity.Finish();

                    ArrivalQueue.Clear();
                    _lastQueued = lastQueued;
                }
            }
            /// <summary>
            /// MUST BE SYNCHRON
            /// GAPS MUST BE ORDERED
            /// </summary>
            internal static void Start(int lastDatabaseId, int lastExecutedId, int[] gaps, System.IO.TextWriter consoleOut)
            {
                consoleOut?.WriteLine("Executing unprocessed activities. {0}-{1} {2}", lastExecutedId, lastDatabaseId, IndexingActivityStatus.GapsToString(gaps, 5, 3));

                SnLog.WriteInformation("Executing unprocessed activities.",
                    EventId.RepositoryRuntime,
                    properties: new Dictionary<string, object>{
                        {"LastDatabaseId", lastDatabaseId},
                        {"LastExecutedId", lastExecutedId},
                        {"CountOfGaps", gaps.Length},
                        {"Gaps", IndexingActivityStatus.GapsToString(gaps, 100, 3)}
                    });

                IndexingActivityBase.NotIndexableContentCollection.Clear();
                DependencyManager.Start();

                var count = 0;
                if (gaps.Any())
                {
                    var loadedActivities = new IndexingActivityLoader(gaps, true);
                    foreach (var indexingActivity in loadedActivities)
                    {
                        var loadedActivity = (IndexingActivityBase) indexingActivity;
                        // wait and start processing loaded activities in the meantime
                        WaitIfOverloaded(true);

                        SnTrace.IndexQueue.Write("IAQ: Startup: A{0} enqueued from db.", loadedActivity.Id);

                        IndexingActivityHistory.Arrive(loadedActivity);
                        ArrivalQueue.Enqueue(loadedActivity);
                        _lastQueued = loadedActivity.Id;
                        count++;
                    }
                }
                if (lastExecutedId < lastDatabaseId)
                {
                    var loadedActivities = new IndexingActivityLoader(lastExecutedId + 1, lastDatabaseId, true);
                    foreach (var indexingActivity in loadedActivities)
                    {
                        var loadedActivity = (IndexingActivityBase) indexingActivity;
                        // wait and start processing loaded activities in the meantime
                        WaitIfOverloaded(true);

                        SnTrace.IndexQueue.Write("IAQ: Startup: A{0} enqueued from db.", loadedActivity.Id);

                        IndexingActivityHistory.Arrive(loadedActivity);
                        ArrivalQueue.Enqueue(loadedActivity);
                        _lastQueued = loadedActivity.Id;
                        count++;
                    }
                }

                if (_lastQueued < lastExecutedId)
                    _lastQueued = lastExecutedId;

                // ensure that the arrival activity queue is not empty at this pont.
                DependencyManager.ActivityEnqueued();

                if (lastDatabaseId != 0 || lastExecutedId != 0 || gaps.Any())
                    while (IsWorking())
                        Thread.Sleep(200);

                if (IndexingActivityBase.NotIndexableContentCollection.Any())
                {
                    const int maxWrittenCount = 100;

                    // Collect the first n ContentId, VersionId pairs into one message
                    // (key: version id, value: content id).
                    var data = string.Join("; ", IndexingActivityBase.NotIndexableContentCollection.Take(maxWrittenCount)
                        .OrderBy(x => x.Value)
                        .Select(x => $"{x.Value},{x.Key}"));
                    var firstN = IndexingActivityBase.NotIndexableContentCollection.Count > maxWrittenCount
                        ? $" first {maxWrittenCount}"
                        : string.Empty;

                    // Write one aggregated message
                    SnLog.WriteWarning($"Cannot index {IndexingActivityBase.NotIndexableContentCollection.Count}" +
                                       $" content. The{firstN} related ContentId, VersionId pairs: {data}");

                    // Do not keep memory for unnecessary data
                    IndexingActivityBase.NotIndexableContentCollection.Clear();
                }

                // At this point we know for sure that the original gap is not there anymore.
                // In case there is a false gap (e.g. because there are missing activity ids 
                // in the db) we have to remove these ids manually from the in-memory gap.
                if (gaps.Any())
                {
                    TerminationHistory.RemoveGaps(gaps);

                    // Commit is necessary because otherwise the gap is removed only in memory, but
                    // the index is not updated in the file system.
                    IndexManager.Commit(); // explicit commit
                }

                SnLog.WriteInformation($"Executing unprocessed activities ({count}) finished.", EventId.RepositoryLifecycle);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal static void ShutDown()
            {
                Reset(int.MaxValue);
                DependencyManager.ShutDown();
            }

            internal static bool IsEmpty => ArrivalQueue.Count == 0;

            internal static int QueueLength => ArrivalQueue.Count;

            private static readonly object ArrivalQueueLock = new object();
            private static int _lastQueued;
            private static readonly Queue<IndexingActivityBase> ArrivalQueue = new Queue<IndexingActivityBase>();

            public static void EnqueueActivity(IndexingActivityBase activity)
            {
                SnTrace.IndexQueue.Write("IAQ: A{0} arrived{1}. {2}, {3}", activity.Id, activity.FromReceiver ? " from another computer" : "", activity.GetType().Name, activity.Path);

                IndexingActivityHistory.Arrive(activity);

                lock (ArrivalQueueLock)
                {
                    if (activity.Id <= _lastQueued)
                    {
                        var sameActivity = ArrivalQueue.FirstOrDefault(a => a.Id == activity.Id);
                        if (sameActivity != null)
                        {
                            sameActivity.Attach(activity);

                            SnTrace.IndexQueue.Write("IAQ: A{0} attached to another one in the queue", activity.Id);

                            return;
                        }
                        DependencyManager.AttachOrFinish(activity);
                        return;
                    }

                    if (activity.Id > _lastQueued + 1)
                    {
                        var from = _lastQueued + 1;
                        var to = activity.Id - 1;
                        var expectedCount = to - from + 1;
                        var loadedActivities = Retrier.Retry(
                            3,
                            100,
                            () => LoadActivities(from, to),
                            (r, i, e) =>
                            {

                                if (i < 3)
                                    SnTrace.IndexQueue.Write("IAQ: Loading attempt {0}", 4 - i);

                                if (e != null)
                                    return false;
                                return r.Count() == expectedCount;
                            });

                        foreach (var indexingActivity in loadedActivities)
                        {
                            var loadedActivity = (IndexingActivityBase) indexingActivity;
                            IndexingActivityHistory.Arrive(loadedActivity);
                            ArrivalQueue.Enqueue(loadedActivity);
                            _lastQueued = loadedActivity.Id;

                            SnTrace.IndexQueue.Write("IAQ: A{0} enqueued from db.", loadedActivity.Id);

                            DependencyManager.ActivityEnqueued();
                        }
                    }
                    ArrivalQueue.Enqueue(activity);
                    _lastQueued = activity.Id;

                    SnTrace.IndexQueue.Write("IAQ: A{0} enqueued.", activity.Id);

                    DependencyManager.ActivityEnqueued();
                }
            }
            public static IndexingActivityBase DequeueActivity()
            {
                lock (ArrivalQueueLock)
                {
                    if (ArrivalQueue.Count == 0)
                        return null;
                    var activity = ArrivalQueue.Dequeue();

                    SnTrace.IndexQueue.Write("IAQ: A{0} dequeued.", activity.Id);

                    return activity;
                }
            }

            private static IEnumerable<IIndexingActivity> LoadActivities(int from, int to)
            {
                SnTrace.IndexQueue.Write("IAQ: Loading activities {0} - {1}", from, to);

                return new IndexingActivityLoader(from, to, false);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal static IndexingActivitySerializerState GetCurrentState()
            {
                lock (ArrivalQueueLock)
                    return new IndexingActivitySerializerState
                    {
                        LastQueued = _lastQueued,
                        Queue = ArrivalQueue.Select(x => x.Id).ToArray()
                    };
            }
        }

        private static class DependencyManager
        {
            internal static void Reset()
            {
                // Before call ensure that the arrival queue is empty.
                lock (WaitingSetLock)
                {
                    if (WaitingSet.Count > 0)
                        SnTrace.IndexQueue.Write("IAQ: RESET: WaitingSet.Count: {0}", WaitingSet.Count);

                    foreach (var activity in WaitingSet)
                        activity.Finish();
                    WaitingSet.Clear();
                }
            }
            internal static void Start()
            {
                lock (WaitingSetLock)
                    WaitingSet.Clear();
            }
            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal static void ShutDown()
            {
                Reset();
            }
            internal static bool IsEmpty => WaitingSet.Count == 0;

            internal static int WaitingSetLength => WaitingSet.Count;

            private static readonly object WaitingSetLock = new object();
            private static readonly List<IndexingActivityBase> WaitingSet = new List<IndexingActivityBase>();

            private static bool _run;
            public static void ActivityEnqueued()
            {
                if (_run)
                    return;
                _run = true;

                System.Threading.Tasks.Task.Run(() => ProcessActivities());
            }

            private static void ProcessActivities()
            {
                while (true)
                {
                    var newerActivity = Serializer.DequeueActivity();
                    if (newerActivity == null)
                    {
                        _run = false;
                        return;
                    }
                    MakeDependencies(newerActivity);
                }
            }
            private static void MakeDependencies(IndexingActivityBase newerActivity)
            {
                lock (WaitingSetLock)
                {
                    foreach (var olderActivity in WaitingSet)
                    {
                        if (MustWait(newerActivity, olderActivity))
                        {
                            newerActivity.WaitFor(olderActivity);

                            SnTrace.IndexQueue.Write("IAQ: A{0} depends from A{1}", newerActivity.Id, olderActivity.Id);

                            IndexingActivityHistory.Wait(newerActivity);
                        }
                    }

                    WaitingSet.Add(newerActivity);

                    if (newerActivity.WaitingFor.Count == 0)
                        System.Threading.Tasks.Task.Run(() => Executor.Execute(newerActivity));
                }
            }
            private static bool MustWait(IndexingActivityBase newerActivity, IndexingActivityBase olderActivity)
            {
                Debug.Assert(olderActivity.Id != newerActivity.Id);

                if (olderActivity.NodeId == newerActivity.NodeId)
                    return true;

                if (olderActivity.Path == newerActivity.Path)
                    return true;

                if (olderActivity is TreeIndexingActivity)
                {
                    if (newerActivity is TreeIndexingActivity)
                        return olderActivity.IsInTree(newerActivity) || newerActivity.IsInTree(olderActivity);
                    else
                        return newerActivity.IsInTree(olderActivity);
                }
                return newerActivity is TreeIndexingActivity && olderActivity.IsInTree(newerActivity);
            }

            internal static void Finish(IndexingActivityBase activity)
            {
                lock (WaitingSetLock)
                {
                    // activity is done in the ActivityQueue
                    WaitingSet.Remove(activity);

                    // terminate and release waiting threads if there is any.
                    activity.Finish();

                    // register activity termination in the log.
                    IndexingActivityHistory.Finish(activity.Id);

                    // register activity termination if the activity was not skipped.
                    if(activity.Executed)
                        TerminationHistory.FinishActivity(activity);

                    // execute all activities that are completely freed.
                    foreach (var dependentItem in activity.WaitingForMe.ToArray())
                    {
                        dependentItem.FinishWaiting(activity);
                        if (dependentItem.WaitingFor.Count == 0)
                            System.Threading.Tasks.Task.Run(() => Executor.Execute(dependentItem));
                    }
                }
            }
            internal static void AttachOrFinish(IndexingActivityBase activity)
            {
                lock (WaitingSetLock)
                {
                    var sameActivity = WaitingSet.FirstOrDefault(a => a.Id == activity.Id);
                    if (sameActivity != null)
                    {
                        sameActivity.Attach(activity);

                        SnTrace.IndexQueue.Write("IAQ: A{0} attached to another in the waiting set.", activity.Id);

                        return;
                    }
                }
                activity.Finish(); // release blocked thread
                IndexingActivityHistory.Finish(activity.Id);

                SnTrace.IndexQueue.Write("IAQ: A{0} ignored: finished but not executed.", activity.Id);

            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static IndexingActivityDependencyState GetCurrentState()
            {
                lock (WaitingSetLock)
                    return new IndexingActivityDependencyState { WaitingSet = WaitingSet.Select(x => x.Id).ToArray() };
            }    
        }

        private static class TerminationHistory
        {
            private static readonly object GapsLock = new object();
            private static int _lastId;
            private static readonly List<int> Gaps = new List<int>();

            internal static void Reset(int lastId, IEnumerable<int> gaps)
            {
                lock (GapsLock)
                {
                    _lastId = lastId;
                    Gaps.Clear();
                    Gaps.AddRange(gaps);
                }
            }

            internal static void FinishActivity(IndexingActivityBase activity)
            {
                var id = activity.Id;
                lock (GapsLock)
                {
                    if (id > _lastId)
                    {
                        if (id > _lastId + 1)
                            Gaps.AddRange(Enumerable.Range(_lastId + 1, id - _lastId - 1));
                        _lastId = id;
                    }
                    else
                    {
                        Gaps.Remove(id);
                    }

                    SnTrace.IndexQueue.Write("IAQ: State after finishing A{0}: {1}", id, GetCurrentState());
                }
            }

            internal static void RemoveGaps(IEnumerable<int> gaps)
            {
                lock (GapsLock)
                {
                    Gaps.RemoveAll(gaps.Contains);
                }
            }
            public static int GetLastTerminatedId()
            {
                return _lastId;
            }
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static IndexingActivityStatus GetCurrentState()
            {
                lock (GapsLock)
                    return new IndexingActivityStatus { LastActivityId = _lastId, Gaps = Gaps.ToArray() };
            }
        }

        private static class Executor
        {
            public static void Execute(IndexingActivityBase activity)
            {
                using (var op = SnTrace.Index.StartOperation("IAQ: A{0} EXECUTION.", activity.Id))
                {
                    IndexingActivityHistory.Start(activity.Id);
                    try
                    {
                        using (new Storage.Security.SystemAccount())
                            activity.ExecuteIndexingActivity();
                    }
                    catch (Exception e)
                    {
                        SnLog.WriteException(e, $"Indexing activity execution error. Activity: #{activity.Id} ({activity.ActivityType})");
                        SnTrace.Index.WriteError("IAQ: A{0} EXECUTION ERROR: {1}", activity.Id, e);
                        IndexingActivityHistory.Error(activity.Id, e);
                    }
                    finally
                    {
                        DependencyManager.Finish(activity);
                        IndexManager.ActivityFinished(activity.Id);
                    }
                    op.Successful = true;
                }
            }
        }

        private class IndexingActivityLoader : IEnumerable<IIndexingActivity>
        {
            private readonly bool _gapLoader;

            private readonly int _from;
            private readonly int _to;
            private readonly int _pageSize;
            private readonly int[] _gaps;
            private readonly bool _executingUnprocessedActivities;

            public IndexingActivityLoader(int from, int to, bool executingUnprocessedActivities)
            {
                _gapLoader = false;
                _from = from;
                _to = to;
                _executingUnprocessedActivities = executingUnprocessedActivities;
                _pageSize = IndexingActivityLoadingBufferSize;
            }

            public IndexingActivityLoader(int[] gaps, bool executingUnprocessedActivities)
            {
                _gapLoader = true;
                _gaps = gaps;
                _executingUnprocessedActivities = executingUnprocessedActivities;
                _pageSize = IndexingActivityLoadingBufferSize;
            }

            public IEnumerator<IIndexingActivity> GetEnumerator()
            {
                if (_gapLoader)
                    return new GapLoader(_gaps, _pageSize, _executingUnprocessedActivities);
                return new SectionLoader(_from, _to, _pageSize, _executingUnprocessedActivities);
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class SectionLoader : IEnumerator<IIndexingActivity>
            {
                private int _from;
                private readonly int _to;
                private readonly int _pageSize;

                private readonly IIndexingActivity[] _buffer;
                private int _pointer;
                private bool _isLastPage;
                private int _loadedPageSize;
                private readonly bool _executingUnprocessedActivities;

                public SectionLoader(int from, int to, int pageSize, bool executingUnprocessedActivities)
                {
                    _from = from;
                    _to = to;
                    _pageSize = pageSize;
                    _executingUnprocessedActivities = executingUnprocessedActivities;

                    _buffer = new IIndexingActivity[pageSize];
                    _loadedPageSize = _buffer.Length;
                    _pointer = _buffer.Length - 1;
                }

                public IIndexingActivity Current => _buffer[_pointer];

                object System.Collections.IEnumerator.Current => Current;

                public void Reset()
                {
                    throw new NotSupportedException();
                }
                public void Dispose()
                {
                    // does nothing
                }

                public bool MoveNext()
                {
                    if (++_pointer >= _loadedPageSize)
                    {
                        if (_isLastPage)
                            return false;

                        LoadNextPage(_buffer, out _isLastPage, out _loadedPageSize);
                        if (_isLastPage && _loadedPageSize == 0)
                            return false;

                        _pointer = 0;
                    }
                    return true;
                }
                private void LoadNextPage(IIndexingActivity[] buffer, out bool isLast, out int count)
                {
                    count = 0;

                    foreach (var item in LoadSegment(_from, _to, _pageSize))
                        buffer[count++] = item;

                    if (count < 1)
                    {
                        isLast = true;
                        return;
                    }

                    var last = buffer[count - 1];
                    _from = last.Id + 1;

                    isLast = last.Id >= _to;
                }
                private IEnumerable<IIndexingActivity> LoadSegment(int from, int to, int count)
                {
                    SnTrace.IndexQueue.Write("IAQ: Loading segment: from: {0}, to: {1}, count: {2}.", from, to, count);

                    var segment = DataProvider.Current.LoadIndexingActivities(from, to, count, _executingUnprocessedActivities, IndexingActivityFactory.Instance);

                    SnTrace.IndexQueue.Write("IAQ: Loaded segment: {0}", String.Join(",", segment.Select(x => x.Id)));

                    return segment;
                }
            }
            private class GapLoader : IEnumerator<IIndexingActivity>
            {
                private readonly int[] _gaps;
                private int _gapIndex;
                private readonly List<IIndexingActivity> _buffer = new List<IIndexingActivity>();
                private int _bufferIndex;
                private readonly int _pageSize;
                private readonly bool _executingUnprocessedActivities;

                public GapLoader(int[] gaps, int pageSize, bool executingUnprocessedActivities)
                {
                    _gaps = gaps;
                    _pageSize = pageSize;
                    _bufferIndex = pageSize;
                    _executingUnprocessedActivities = executingUnprocessedActivities;
                }

                public IIndexingActivity Current => _buffer[_bufferIndex];

                object System.Collections.IEnumerator.Current => Current;

                public void Reset()
                {
                    throw new NotSupportedException();
                }
                public void Dispose()
                {
                    // does nothing
                }

                public bool MoveNext()
                {
                    _bufferIndex++;
                    if (_bufferIndex >= _buffer.Count)
                    {
                        LoadNextBuffer();
                        if (_buffer.Count == 0 && _gapIndex >= _gaps.Length)
                            return false;
                        _bufferIndex = 0;
                    }
                    return true;
                }
                private void LoadNextBuffer()
                {
                    _buffer.Clear();
                    while (true)
                    {
                        if (_gapIndex >= _gaps.Length)
                            break;
                        var gapPage = _gaps.Skip(_gapIndex).Take(_pageSize).ToArray();
                        _buffer.AddRange(LoadGaps(gapPage));
                        _gapIndex += _pageSize;
                        if (_buffer.Count >= _pageSize)
                            break;
                    }
                }
                private IEnumerable<IIndexingActivity> LoadGaps(int[] gaps)
                {
                    SnTrace.IndexQueue.Write("IAQ: Loading gaps (count: {0}): [{1}]", gaps.Length, String.Join(", ", gaps));

                    return DataProvider.Current.LoadIndexingActivities(gaps, _executingUnprocessedActivities, IndexingActivityFactory.Instance);
                }
            }
        }
    }

    /// <summary>
    /// Defines a data class that provides information about the activity execution serialization in the population of the local index.
    /// </summary>
    public class IndexingActivitySerializerState
    {
        /// <summary>
        /// Gets or sets the Id of the last queued indexing activity.
        /// </summary>
        public int LastQueued { get; set; }
        /// <summary>
        /// Gets the length of the arrival queue of the indexing activities.
        /// </summary>
        public int QueueLength => Queue?.Length ?? 0;
        /// <summary>
        /// Gets the Ids of the activities in the arrival queue.
        /// </summary>
        public int[] Queue { get; set; }
    }
    /// <summary>
    /// Defines a data class that provides information about the waiting activities in the population of the local index.
    /// </summary>
    public class IndexingActivityDependencyState
    {
        /// <summary>
        /// Gets the length of list that contains waiting indexing activities.
        /// </summary>
        public int WaitingSetLength => WaitingSet?.Length ?? 0;
        /// <summary>
        /// Gets the Ids of the waiting indexing activities.
        /// </summary>
        public int[] WaitingSet { get; set; }
    }
    /// <summary>
    /// Defines a data class that provides information about the indeing activity organizer in the population of the local index.
    /// </summary>
    public class IndexingActivityQueueState
    {
        /// <summary>
        /// Gets or sets the state of the indexing activity serializer.
        /// </summary>
        public IndexingActivitySerializerState Serializer { get; set; }
        /// <summary>
        /// Gets or sets the state of the indexing activity dependency manager.
        /// </summary>
        public IndexingActivityDependencyState DependencyManager { get; set; }
        /// <summary>
        /// Gets or sets the state of the executed indexing activities.
        /// </summary>
        public IndexingActivityStatus Termination { get; set; }
    }

    /// <summary>
    /// Defines a data class that represents an item in the short history of the indexing activity execution in the population of the local index.
    /// </summary>
    public class IndexingActivityHistoryItem
    {
        /// <summary>
        /// Gets or sets the Id of the indexing activity.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the type name of the indexing activity.
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the indexing activity is received from a messaging channel.
        /// </summary>
        public bool FromReceiver { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the indexing activity is loaded from the database.
        /// </summary>
        public bool FromDb { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if the indexing activity is executed in the system startup sequence.
        /// </summary>
        public bool IsStartup { get; set; }
        /// <summary>
        /// Gets or sets a value that is true if any error occured in the execution of the indexing activity.
        /// </summary>
        public string Error { get; set; }
        /// <summary>
        /// Gets or sets an Id array of the indexing activities that are blocked the current activity's execution.
        /// </summary>
        public int[] WaitedFor { get; set; }
        /// <summary>
        /// Gets or sets the arrival time of the indexing activity.
        /// </summary>
        public DateTime ArrivedAt { get; set; }
        /// <summary>
        /// Gets or sets the starting time of the indexing activity execution.
        /// </summary>
        public DateTime StartedAt { get; set; }
        /// <summary>
        /// Gets or sets the finishing time of the indexing activity execution.
        /// </summary>
        public DateTime FinishedAt { get; set; }
        /// <summary>
        /// Gets the waiting time of the indexing activity.
        /// </summary>
        public TimeSpan WaitTime => StartedAt - ArrivedAt;
        /// <summary>
        /// Gets the execution time of the indexing activity.
        /// </summary>
        public TimeSpan ExecTime => FinishedAt - StartedAt;
        /// <summary>
        /// Gets the full time of the indexing activity.
        /// </summary>
        public TimeSpan FullTime => FinishedAt - ArrivedAt;
    }
    /// <summary>
    /// Defines a data class that provides information about the short history of the indexing activity execution in the population of the local index.
    /// </summary>
    public class IndexingActivityHistory
    {
        /// <summary>
        /// Gets or sets the state of the indexing activity organizer.
        /// </summary>
        public IndexingActivityQueueState State { get; private set; }
        /// <summary>
        /// Gets a message that occurs when there are one or more unfinished history item.
        /// This is happens tipically in case of the webserver's heavy load.
        /// </summary>
        public string Message => _unfinished < 1 ? null : ("RECENT ARRAY TOO SHORT. Cannot registrate full activity lifecycle. Unfinished items: " + _unfinished);
        /// <summary>
        /// Gets the length of the recent list.
        /// </summary>
        public int RecentLength => Recent?.Length ?? 0;
        /// <summary>
        /// Gets the last relevant items in the history.
        /// </summary>
        public IndexingActivityHistoryItem[] Recent { get; private set; }

        private IndexingActivityHistory() { }

        internal string GetJson()
        {
            try
            {
                var writer = new System.IO.StringWriter();
                WriteJson(writer);
                return writer.GetStringBuilder().ToString();
            }
            catch
            {
                return "SERIALIZATION ERROR";
            }
        }
        internal void WriteJson(System.IO.TextWriter writer)
        {
            JsonSerializer.Create(new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            })
            .Serialize(writer, this);
        }

        /// <summary>
        /// Generates a history from the recent items.
        /// </summary>
        /// <returns></returns>
        public static IndexingActivityHistory GetHistory()
        {
            IndexingActivityHistory result;
            var list = new List<IndexingActivityHistoryItem>(History.Length);
            lock (Lock)
            {
                for (int i = _position; i < History.Length; i++)
                    if (History[i] != null)
                        list.Add(History[i]);
                for (int i = 0; i < _position; i++)
                    if (History[i] != null)
                        list.Add(History[i]);

                result = new IndexingActivityHistory()
                {
                    State = DistributedIndexingActivityQueue.GetCurrentState(),
                    Recent = list.ToArray()
                };
            }
            return result;
        }
        /// <summary>
        /// Resets the history and returns with the starting state.
        /// </summary>
        public static IndexingActivityHistory Reset()
        {
            IndexingActivityHistory result;
            lock (Lock)
            {
                for (int i = 0; i < History.Length; i++)
                    History[i] = null;

                _position = 0;
                _unfinished = 0;

                result = new IndexingActivityHistory()
                {
                    State = DistributedIndexingActivityQueue.GetCurrentState(),
                    Recent = new IndexingActivityHistoryItem[0]
                };
            }
            return result;
        }

        private static readonly object Lock = new object();
        private const int HistoryLength = 1023;
        private static readonly IndexingActivityHistoryItem[] History = new IndexingActivityHistoryItem[HistoryLength];
        private static int _position;
        private static int _unfinished;

        internal static void Arrive(IndexingActivityBase activity)
        {
            lock (Lock)
            {
                // avoiding duplication
                foreach (var item in History)
                    if (item != null && item.Id == activity.Id)
                        return;

                var retired = History[_position];
                History[_position] = new IndexingActivityHistoryItem
                {
                    Id = activity.Id,
                    TypeName = activity.ActivityType.ToString(),
                    FromReceiver = activity.FromReceiver,
                    FromDb = activity.FromDatabase,
                    IsStartup = activity.IsUnprocessedActivity,
                    ArrivedAt = DateTime.UtcNow,
                    StartedAt = DateTime.MinValue,
                    FinishedAt = DateTime.MinValue
                };

                if (retired != null)
                    if (retired.FinishedAt == DateTime.MinValue)
                        _unfinished++;

                _position++;
                if (_position >= HistoryLength)
                    _position = 0;
            }
        }
        internal static void Wait(IndexingActivityBase activity)
        {
            lock (Lock)
            {
                foreach (var item in History)
                {
                    if (item != null && item.Id == activity.Id)
                    {
                        item.WaitedFor = activity.WaitingFor.Select(a => a.Id).ToArray();
                        break;
                    }
                }
            }
        }
        internal static void Start(int activityId)
        {
            lock (Lock)
            {
                foreach (var item in History)
                {
                    if (item != null && item.Id == activityId)
                    {
                        item.StartedAt = DateTime.UtcNow;
                        return;
                    }
                }
                SnTrace.IndexQueue.Write("IAQ: A{0} DOES NOT FOUND IN HISTORY. Cannot start.", activityId);
            }
        }
        internal static void Finish(int activityId)
        {
            lock (Lock)
            {
                foreach (var item in History)
                {
                    if (item != null && item.Id == activityId)
                    {
                        item.FinishedAt = DateTime.UtcNow;
                        return;
                    }
                }
            }
            SnTrace.IndexQueue.Write("IAQ: A{0} DOES NOT FOUND IN HISTORY. Cannot stop.", activityId);
        }
        internal static void Error(int activityId, Exception e)
        {
            lock (Lock)
            {
                foreach (var item in History)
                {
                    if (item != null && item.Id == activityId)
                    {
                        item.Error = e.GetType().Name + ": " + e;
                        return;
                    }
                }
            }

            SnTrace.IndexQueue.Write("IAQ: A{0} DOES NOT FOUND IN HISTORY. Cannot register an error.", activityId);
        }
    }
}

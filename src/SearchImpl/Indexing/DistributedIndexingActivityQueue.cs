using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing.Activities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using SenseNet.ContentRepository;
using SenseNet.Search.Indexing;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Search.Indexing
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

                foreach (IndexingActivityBase activity in new IndexingActivityLoader(state.Gaps, false))
                {
                    WaitIfOverloaded();
                    DistributedIndexingActivityQueue.ExecuteActivity(activity);
                }
            }

            var lastId = TerminationHistory.GetLastTerminatedId();
            var lastDbId = IndexManager.GetLastStoredIndexingActivityId();
            var newerCount = lastDbId - lastId;
            if (lastId < lastDbId)
            {

                SnTrace.IndexQueue.Write("IAQ: Health checker is processing activities from {0} to {1}", (lastId + 1), lastDbId);

                foreach (IndexingActivityBase activity in new IndexingActivityLoader(lastId + 1, lastDbId, false))
                {
                    WaitIfOverloaded();
                    DistributedIndexingActivityQueue.ExecuteActivity(activity);
                }
            }
        }

        private static void WaitIfOverloaded(bool startProcessing = false)
        {
            // We prevent memory overflow by limiting the number of activities that we
            // keep in memory. This method waits for the queue to be able to process
            // new activities.
            var logCount = 1;
            while (DistributedIndexingActivityQueue.IsOverloaded())
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
            return DependencyManager.WaitingSetLength >= SenseNet.Configuration.Indexing.IndexingActivityQueueMaxLength ||
                Serializer.QueueLength >= SenseNet.Configuration.Indexing.IndexingActivityQueueMaxLength;
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
                lock (_arrivalQueueLock)
                {

                    if (_arrivalQueue.Count > 0)
                        SnTrace.IndexQueue.Write("IAQ: RESET: ArrivalQueue.Count: {0}", _arrivalQueue.Count);


                    foreach (var activity in _arrivalQueue)
                        activity.Finish();
                    _arrivalQueue.Clear();
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

                DependencyManager.Start();

                var count = 0;
                if (gaps.Any())
                {
                    var loadedActivities = new IndexingActivityLoader(gaps, true);
                    foreach (IndexingActivityBase loadedActivity in loadedActivities)
                    {
                        // wait and start processing loaded activities in the meantime
                        WaitIfOverloaded(true);

                        SnTrace.IndexQueue.Write("IAQ: Startup: A{0} enqueued from db.", loadedActivity.Id);

                        IndexingActivityHistory.Arrive(loadedActivity);
                        _arrivalQueue.Enqueue(loadedActivity);
                        _lastQueued = loadedActivity.Id;
                        count++;
                    }
                }
                if (lastExecutedId < lastDatabaseId)
                {
                    var loadedActivities = new IndexingActivityLoader(lastExecutedId + 1, lastDatabaseId, true);
                    foreach (IndexingActivityBase loadedActivity in loadedActivities)
                    {
                        // wait and start processing loaded activities in the meantime
                        WaitIfOverloaded(true);

                        SnTrace.IndexQueue.Write("IAQ: Startup: A{0} enqueued from db.", loadedActivity.Id);

                        IndexingActivityHistory.Arrive(loadedActivity);
                        _arrivalQueue.Enqueue(loadedActivity);
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
            internal static void ShutDown()
            {
                Reset(int.MaxValue);
                DependencyManager.ShutDown();
            }

            internal static bool IsEmpty
            {
                get { return _arrivalQueue.Count == 0; }
            }
            internal static int QueueLength
            {
                get { return _arrivalQueue.Count; }
            }

            private static object _arrivalQueueLock = new object();
            private static int _lastQueued;
            private static Queue<IndexingActivityBase> _arrivalQueue = new Queue<IndexingActivityBase>();

            public static void EnqueueActivity(IndexingActivityBase activity)
            {

                SnTrace.IndexQueue.Write("IAQ: A{0} arrived{1}. {2}, {3}", activity.Id, activity.FromReceiver ? " from another computer" : "", activity.GetType().Name, activity.Path);

                IndexingActivityHistory.Arrive(activity);

                lock (_arrivalQueueLock)
                {
                    if (activity.Id <= _lastQueued)
                    {
                        var sameActivity = _arrivalQueue.FirstOrDefault(a => a.Id == activity.Id);
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
                        var loadedActivities = Retrier.Retry<IEnumerable<IIndexingActivity>>(
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

                        foreach (IndexingActivityBase loadedActivity in loadedActivities)
                        {
                            IndexingActivityHistory.Arrive(loadedActivity);
                            _arrivalQueue.Enqueue(loadedActivity);
                            _lastQueued = loadedActivity.Id;

                            SnTrace.IndexQueue.Write("IAQ: A{0} enqueued from db.", loadedActivity.Id);

                            DependencyManager.ActivityEnqueued();
                        }
                    }
                    _arrivalQueue.Enqueue(activity);
                    _lastQueued = activity.Id;

                    SnTrace.IndexQueue.Write("IAQ: A{0} enqueued.", activity.Id);

                    DependencyManager.ActivityEnqueued();
                }
            }
            public static IndexingActivityBase DequeueActivity()
            {
                lock (_arrivalQueueLock)
                {
                    if (_arrivalQueue.Count == 0)
                        return null;
                    var activity = _arrivalQueue.Dequeue();

                    SnTrace.IndexQueue.Write("IAQ: A{0} dequeued.", activity.Id);

                    return activity;
                }
            }

            private static IEnumerable<IIndexingActivity> LoadActivities(int from, int to)
            {

                SnTrace.IndexQueue.Write("IAQ: Loading activities {0} - {1}", from, to);

                return new IndexingActivityLoader(from, to, false);
            }

            internal static IndexingActivitySerializerState GetCurrentState()
            {
                lock (_arrivalQueueLock)
                    return new IndexingActivitySerializerState
                    {
                        LastQueued = _lastQueued,
                        Queue = _arrivalQueue.Select(x => x.Id).ToArray()
                    };
            }
        }

        private static class DependencyManager
        {
            internal static void Reset()
            {
                // Before call ensure that the arrival queue is empty.
                lock (_waitingSetLock)
                {

                    if (_waitingSet.Count > 0)
                        SnTrace.IndexQueue.Write("IAQ: RESET: WaitingSet.Count: {0}", _waitingSet.Count);


                    foreach (var activity in _waitingSet)
                        activity.Finish();
                    _waitingSet.Clear();
                }
            }
            internal static void Start()
            {
                lock (_waitingSetLock)
                    _waitingSet.Clear();
            }
            internal static void ShutDown()
            {
                Reset();
            }
            internal static bool IsEmpty
            {
                get { return _waitingSet.Count == 0; }
            }
            internal static int WaitingSetLength
            {
                get { return _waitingSet.Count; }
            }

            private static object _waitingSetLock = new object();
            private static List<IndexingActivityBase> _waitingSet = new List<IndexingActivityBase>();

            private static bool _run;
            public static void ActivityEnqueued()
            {
                if (_run)
                    return;
                _run = true;
                var x = Task.Run(() => ProcessActivities());
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
                lock (_waitingSetLock)
                {
                    foreach (var olderActivity in _waitingSet)
                    {
                        if (MustWait(newerActivity, olderActivity))
                        {
                            newerActivity.WaitFor(olderActivity);

                            SnTrace.IndexQueue.Write("IAQ: A{0} depends from A{1}", newerActivity.Id, olderActivity.Id);

                            IndexingActivityHistory.Wait(newerActivity);
                        }
                    }

                    _waitingSet.Add(newerActivity);

                    if (newerActivity.WaitingFor.Count == 0)
                        Task.Run(() => Executor.Execute(newerActivity));
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
                lock (_waitingSetLock)
                {
                    // activity is done in the ActivityQueue
                    _waitingSet.Remove(activity);

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
                            Task.Run(() => Executor.Execute(dependentItem));
                    }
                }
            }
            internal static void AttachOrFinish(IndexingActivityBase activity)
            {
                lock (_waitingSetLock)
                {
                    var sameActivity = _waitingSet.FirstOrDefault(a => a.Id == activity.Id);
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

            public static IndexingActivityDependencyState GetCurrentState()
            {
                lock (_waitingSetLock)
                    return new IndexingActivityDependencyState { WaitingSet = _waitingSet.Select(x => x.Id).ToArray() };
            }    
        }

        private static class TerminationHistory
        {
            private static object _gapsLock = new object();
            private static int _lastId;
            private static List<int> _gaps = new List<int>();

            internal static void Reset(int lastId, IEnumerable<int> gaps)
            {
                lock (_gapsLock)
                {
                    _lastId = lastId;
                    _gaps.Clear();
                    _gaps.AddRange(gaps);
                }
            }

            internal static void FinishActivity(IndexingActivityBase activity)
            {
                var id = activity.Id;
                lock (_gapsLock)
                {
                    if (id > _lastId)
                    {
                        if (id > _lastId + 1)
                            _gaps.AddRange(Enumerable.Range(_lastId + 1, id - _lastId - 1));
                        _lastId = id;
                    }
                    else
                    {
                        _gaps.Remove(id);
                    }

                    SnTrace.IndexQueue.Write("IAQ: State after finishing A{0}: {1}", id, GetCurrentState());
                }
            }

            internal static void RemoveGaps(IEnumerable<int> gaps)
            {
                lock (_gapsLock)
                {
                    _gaps.RemoveAll(gaps.Contains);
                }
            }
            public static int GetLastTerminatedId()
            {
                return _lastId;
            }
            public static IndexingActivityStatus GetCurrentState()
            {
                lock (_gapsLock)
                    return new IndexingActivityStatus { LastActivityId = _lastId, Gaps = _gaps.ToArray() };
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
                        using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                            activity.ExecuteIndexingActivity();
                    }
                    catch (Exception e)
                    {
                        //TODO: WARNING Do not fill the event log with repetitive messages.
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
            private bool gapLoader;

            private int from;
            private int to;
            private int pageSize;
            private int[] gaps;
            private bool executingUnprocessedActivities;

            public IndexingActivityLoader(int from, int to, bool executingUnprocessedActivities)
            {
                gapLoader = false;
                this.from = from;
                this.to = to;
                this.executingUnprocessedActivities = executingUnprocessedActivities;
                this.pageSize = DistributedIndexingActivityQueue.IndexingActivityLoadingBufferSize;
            }
            public IndexingActivityLoader(int[] gaps, bool executingUnprocessedActivities)
            {
                this.gapLoader = true;
                this.gaps = gaps;
                this.pageSize = DistributedIndexingActivityQueue.IndexingActivityLoadingBufferSize;
            }

            public IEnumerator<IIndexingActivity> GetEnumerator()
            {
                if (gapLoader)
                    return new GapLoader(this.gaps, this.pageSize, this.executingUnprocessedActivities);
                return new SectionLoader(this.from, this.to, this.pageSize, this.executingUnprocessedActivities);
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class SectionLoader : IEnumerator<IIndexingActivity>
            {
                private int from;
                private int to;
                private int pageSize;

                private IIndexingActivity[] buffer;
                private int pointer;
                private bool isLastPage;
                private int loadedPageSize;
                private bool executingUnprocessedActivities;

                public SectionLoader(int from, int to, int pageSize, bool executingUnprocessedActivities)
                {
                    this.from = from;
                    this.to = to;
                    this.pageSize = pageSize;
                    this.executingUnprocessedActivities = executingUnprocessedActivities;

                    this.buffer = new IndexingActivityBase[pageSize];
                    this.loadedPageSize = this.buffer.Length;
                    this.pointer = this.buffer.Length - 1;
                }

                public IIndexingActivity Current
                {
                    get { return this.buffer[this.pointer]; }
                }
                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }
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
                    if (++this.pointer >= this.loadedPageSize)
                    {
                        if (this.isLastPage)
                            return false;

                        LoadNextPage(this.buffer, out this.isLastPage, out this.loadedPageSize);
                        if (this.isLastPage && this.loadedPageSize == 0)
                            return false;

                        this.pointer = 0;
                    }
                    return true;
                }
                private void LoadNextPage(IIndexingActivity[] buffer, out bool isLast, out int count)
                {
                    count = 0;

                    foreach (var item in LoadSegment(from, to, pageSize))
                        buffer[count++] = item;

                    if (count < 1)
                    {
                        isLast = true;
                        return;
                    }

                    var last = buffer[count - 1];
                    from = last.Id + 1;

                    isLast = last.Id >= to;
                }
                private IEnumerable<IIndexingActivity> LoadSegment(int from, int to, int count)
                {

                    SnTrace.IndexQueue.Write("IAQ: Loading segment: from: {0}, to: {1}, count: {2}.", from, to, count);

                    var segment = DataProvider.Current.LoadIndexingActivities(from, to, count, executingUnprocessedActivities, IndexingActivityFactory.Instance);

                    SnTrace.IndexQueue.Write("IAQ: Loaded segment: {0}", String.Join(",", segment.Select(x => x.Id)));

                    return segment;
                }

            }
            private class GapLoader : IEnumerator<IIndexingActivity>
            {
                private int[] gaps;
                private int gapIndex = 0;
                private List<IIndexingActivity> buffer = new List<IIndexingActivity>();
                private int bufferIndex = -1;
                private int pageSize;
                private bool executingUnprocessedActivities;

                public GapLoader(int[] gaps, int pageSize, bool executingUnprocessedActivities)
                {
                    this.gaps = gaps;
                    this.pageSize = pageSize;
                    this.bufferIndex = pageSize;
                    this.executingUnprocessedActivities = executingUnprocessedActivities;
                }

                public IIndexingActivity Current
                {
                    get { return this.buffer[this.bufferIndex]; }
                }
                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }
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
                    this.bufferIndex++;
                    if (this.bufferIndex >= this.buffer.Count)
                    {
                        LoadNextBuffer();
                        if (this.buffer.Count == 0 && this.gapIndex >= this.gaps.Length)
                            return false;
                        this.bufferIndex = 0;
                    }
                    return true;
                }
                private void LoadNextBuffer()
                {
                    this.buffer.Clear();
                    while (true)
                    {
                        if (this.gapIndex >= this.gaps.Length)
                            break;
                        var gapPage = this.gaps.Skip(gapIndex).Take(pageSize).ToArray();
                        this.buffer.AddRange(LoadGaps(gapPage));
                        this.gapIndex += pageSize;
                        if (this.buffer.Count >= this.pageSize)
                            break;
                    }
                }
                private IEnumerable<IIndexingActivity> LoadGaps(int[] gaps)
                {

                    SnTrace.IndexQueue.Write("IAQ: Loading gaps (count: {0}): [{1}]", gaps.Length, String.Join(", ", gaps));

                    return DataProvider.Current.LoadIndexingActivities(gaps, executingUnprocessedActivities, IndexingActivityFactory.Instance);
                }

            }
        }
    }

    public class IndexingActivitySerializerState
    {
        public int LastQueued { get; set; }
        public int QueueLength { get { return Queue == null ? 0 : Queue.Length; } }
        public int[] Queue { get; set; }
    }
    public class IndexingActivityDependencyState
    {
        public int WaitingSetLength { get { return WaitingSet == null ? 0 : WaitingSet.Length; } }
        public int[] WaitingSet { get; set; }
    }
    public class IndexingActivityQueueState
    {
        public IndexingActivitySerializerState Serializer { get; set; }
        public IndexingActivityDependencyState DependencyManager { get; set; }
        public IndexingActivityStatus Termination { get; set; }
    }

    public class IndexingActivityHistoryItem
    {
        public int Id { get; set; }
        public string TypeName { get; set; }
        public bool FromReceiver { get; set; }
        public bool FromDb { get; set; }
        public bool IsStartup { get; set; }
        public string Error { get; set; }
        public int[] WaitedFor { get; set; }
        public DateTime ArrivedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public TimeSpan WaitTime { get { return StartedAt - ArrivedAt; } }
        public TimeSpan ExecTime { get { return FinishedAt - StartedAt; } }
        public TimeSpan FullTime { get { return FinishedAt - ArrivedAt; } }
    }
    public class IndexingActivityHistory
    {
        public IndexingActivityQueueState State { get; private set; }
        public string Message { get { return _unfinished < 1 ? null : ("RECENT ARRAY TOO SHORT. Cannot registrate full activity lifecycle. Unfinished items: " + _unfinished); } }
        public int RecentLength { get { return Recent == null ? 0 : Recent.Length; } }
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

        public static IndexingActivityHistory GetHistory()
        {
            IndexingActivityHistory result;
            var list = new List<IndexingActivityHistoryItem>(_history.Length);
            lock (_lock)
            {
                for (int i = _position; i < _history.Length; i++)
                    if (_history[i] != null)
                        list.Add(_history[i]);
                for (int i = 0; i < _position; i++)
                    if (_history[i] != null)
                        list.Add(_history[i]);

                result = new IndexingActivityHistory()
                {
                    State = DistributedIndexingActivityQueue.GetCurrentState(),
                    Recent = list.ToArray()
                };
            }
            return result;
        }
        public static IndexingActivityHistory Reset()
        {
            IndexingActivityHistory result;
            lock (_lock)
            {
                for (int i = 0; i < _history.Length; i++)
                    _history[i] = null;

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

        private static object _lock = new object();
        private const int HistoryLength = 1023;
        private static IndexingActivityHistoryItem[] _history = new IndexingActivityHistoryItem[HistoryLength];
        private static int _position = 0;
        private static int _unfinished;

        internal static void Arrive(IndexingActivityBase activity)
        {
            lock (_lock)
            {
                // avoiding duplication
                foreach (var item in _history)
                    if (item != null && item.Id == activity.Id)
                        return;

                var retired = _history[_position];
                _history[_position] = new IndexingActivityHistoryItem
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
            lock (_lock)
            {
                foreach (var item in _history)
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
            lock (_lock)
            {
                foreach (var item in _history)
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
            lock (_lock)
            {
                foreach (var item in _history)
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
            lock (_lock)
            {
                foreach (var item in _history)
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

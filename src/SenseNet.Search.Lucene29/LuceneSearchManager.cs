using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using SenseNet.Diagnostics;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

namespace SenseNet.Search.Lucene29
{
    /// <summary>
    /// Encapsulates all low-level Lucene indexing operations. Has direct Lucene29 references. Create a single instance
    /// of this class when creating a Lucene29 indexing engine implementation in the app domain where the actual index
    /// folder is stored.
    /// </summary>
    public class LuceneSearchManager
    {
        public static readonly Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_29;

        public bool Running { get; set; }
        public IndexDirectory IndexDirectory { get; }

        public IDictionary<string, Analyzer> AnalyzerInfo { get; private set; }
        public IDictionary<string, IndexValueType> IndexFieldTypeInfo { get; private set; }

        private DateTime IndexReopenedAt { get; set; }

        private TimeSpan _forceReopenFrequency;
        public TimeSpan ForceReopenFrequency
        {
            get
            {
                if (_forceReopenFrequency == default(TimeSpan))
                    _forceReopenFrequency = TimeSpan.FromSeconds(30);

                return _forceReopenFrequency;
            }
            set => _forceReopenFrequency = value;
        }

        private string NotificationSender { get; }

        //================================================================================== Constructor

        public LuceneSearchManager(IndexDirectory indexDirectory, string notificationSender = null)
        {
            IndexDirectory = indexDirectory;
            NotificationSender = notificationSender;
        }

        //================================================================================== Startup/Shutdown

        public event Action<TextWriter> OnStarted;
        public event Action OnLockFileRemoved;

        private readonly object _startSync = new object();
        public void Start(TextWriter consoleOut)
        {
            if (!Running)
            {
                lock (_startSync)
                {
                    if (!Running)
                    {
                        Startup(consoleOut);
                        OnStarted?.Invoke(consoleOut);
                        Running = true;
                    }
                }
            }
        }
        [SuppressMessage("ReSharper", "UnusedVariable")]
        private void Startup(TextWriter consoleOut)
        {
            WaitForWriterLockFileIsReleased(WaitForLockFileType.OnStart);

            RemoveIndexWriterLockFile(consoleOut);

            IndexWriter.SetDefaultWriteLockTimeout(20 * 60 * 1000); // 20 minutes
            
            OnLockFileRemoved?.Invoke();

            // Lucene subsystem behaves strangely if the enums are not initialized.
            var x = Field.Index.NO;
            var y = Field.Store.NO;
            var z = Field.TermVector.NO;

            CreateWriterAndReader();
        }

        public void ShutDown()
        {
            using (var op = SnTrace.Index.StartOperation("LUCENEMANAGER SHUTDOWN"))
            {
                if (_writer != null)
                {
                    lock (_commitLock)
                        Commit(false);

                    using (var op2 = SnTrace.Index.StartOperation("LM.CloseReaders"))
                    {
                        using (IndexWriterFrame.Get(_writer, _writerRestartLock, true))
                        {
                            _reader?.Close();
                            _writer?.Close();
                            Running = false;

                            _writer = null;
                            _reader = null;
                        }
                        op2.Successful = true;
                    }
                }
                op.Successful = true;
            }
            using (var op3 = SnTrace.Index.StartOperation("LM.Waiting for writer lock file is released."))
            {
                WaitForWriterLockFileIsReleased(WaitForLockFileType.OnEnd);
                op3.Successful = true;
            }
        }

        public void ClearIndex()
        {
            _reader?.Close();
            _writer?.Close();

            var dir = FSDirectory.Open(new DirectoryInfo(IndexDirectory.CurrentDirectory));
            var writer = new IndexWriter(dir, GetAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
            writer.Commit();
            writer.Close();

            CreateWriterAndReader();
        }

        public IndexingActivityStatus ReadActivityStatusFromIndex()
        {
            using (var readerFrame = GetIndexReaderFrame())
                return ParseFromReader(readerFrame.IndexReader);
        }

        public void WriteActivityStatusToIndex(IndexingActivityStatus state)
        {
            Commit(true, state);
        }

        //================================================================================== Lock file operationss

        public enum WaitForLockFileType { OnStart = 0, OnEnd }

        private const string WAITINGFORLOCKSTR = "write.lock exists, waiting for removal...";
        private const string WRITELOCKREMOVEERRORSUBJECTSTR = "Error at application start";
        private const string WRITELOCKREMOVEERRORTEMPLATESTR = "Write.lock was present at application start and was not removed within set timeout interval ({0} seconds) - a previous appdomain may use the index. Write.lock deletion and application start is forced. AppDomain friendlyname: {1}, base directory: {2}";
        private const string WRITELOCKREMOVEERRORONENDTEMPLATESTR = "Write.lock was present at shutdown and was not removed within set timeout interval ({0} seconds) - application exit is forced. AppDomain friendlyname: {1}, base directory: {2}";
        private const string WRITELOCKREMOVEEMAILERRORSTR = "Could not send notification email about write.lock removal. Check the notification section in the config file!";
        
        /// <summary>
        /// Waits for write.lock to disappear for a configured time interval. Timeout: configured with IndexLockFileWaitForRemovedTimeout key. 
        /// If timeout is exceeded an error is logged and execution continues. For errors at OnStart an email is also sent to a configured address.
        /// </summary>
        /// <param name="waitType">A parameter that influences the logged error message and email template only.</param>
        public void WaitForWriterLockFileIsReleased(WaitForLockFileType waitType)
        {
            // check if writer.lock is still there -> if yes, wait for other appdomain to quit or lock to disappear - until a given timeout.
            // after timeout is passed, Repository.Start will deliberately attempt to remove lock file on following startup

            if (!WaitForWriterLockFileIsReleased())
            {
                // lock file was not removed by other or current appdomain for the given time interval (onstart: other appdomain might use it, onend: current appdomain did not release it yet)
                // onstart -> notify operator and start repository anyway
                // onend -> log error, and continue
                var template = waitType == WaitForLockFileType.OnEnd ? WRITELOCKREMOVEERRORONENDTEMPLATESTR : WRITELOCKREMOVEERRORTEMPLATESTR;
                SnLog.WriteError(string.Format(template, Configuration.Lucene29.IndexLockFileWaitForRemovedTimeout,
                    AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.BaseDirectory));

                if (waitType == WaitForLockFileType.OnStart)
                    SendWaitForLockErrorMail();
            }
        }
        private void SendWaitForLockErrorMail()
        {
            if (!string.IsNullOrEmpty(NotificationSender) && !String.IsNullOrEmpty(Configuration.Lucene29.IndexLockFileRemovedNotificationEmail))
            {
                try
                {
                    var smtpClient = new SmtpClient();
                    var msgstr = String.Format(WRITELOCKREMOVEERRORTEMPLATESTR,
                        Configuration.Lucene29.IndexLockFileWaitForRemovedTimeout,
                        AppDomain.CurrentDomain.FriendlyName,
                        AppDomain.CurrentDomain.BaseDirectory);
                    var msg = new MailMessage(
                        NotificationSender,
                        Configuration.Lucene29.IndexLockFileRemovedNotificationEmail.Replace(';', ','),
                        WRITELOCKREMOVEERRORSUBJECTSTR,
                        msgstr);
                    smtpClient.Send(msg);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }
            }
            else
            {
                SnLog.WriteError(WRITELOCKREMOVEEMAILERRORSTR);
            }
        }

        /// <summary>
        /// Waits for releasing index writer lock file in the configured index directory. Timeout: configured with IndexLockFileWaitForRemovedTimeout key.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public bool WaitForWriterLockFileIsReleased()
        {
            return WaitForWriterLockFileIsReleased(IndexDirectory.CurrentDirectory);
        }
        /// <summary>
        /// Waits for releasing index writer lock file in the specified directory. Timeout: configured with IndexLockFileWaitForRemovedTimeout key.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public static bool WaitForWriterLockFileIsReleased(string indexDirectory)
        {
            return WaitForWriterLockFileIsReleased(indexDirectory, Configuration.Lucene29.IndexLockFileWaitForRemovedTimeout);
        }
        /// <summary>
        /// Waits for releasing index writer lock file in the specified directory and timeout.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public static bool WaitForWriterLockFileIsReleased(string indexDirectory, int timeout)
        {
            if (indexDirectory == null)
            {
                SnTrace.Repository.Write("Index directory not found.");
                return true;
            }

            var lockFilePath = Path.Combine(indexDirectory, IndexWriter.WRITE_LOCK_NAME);
            var deadline = DateTime.UtcNow.AddSeconds(timeout);

            SnTrace.Repository.Write("Waiting for lock file to disappear: " + lockFilePath);

            while (File.Exists(lockFilePath))
            {
                Trace.WriteLine(WAITINGFORLOCKSTR);
                SnTrace.Repository.Write(WAITINGFORLOCKSTR);

                Thread.Sleep(100);
                if (DateTime.UtcNow > deadline)
                    return false;
            }

            SnTrace.Repository.Write("Lock file has gone: " + lockFilePath);

            return true;
        }


        /// <summary>
        /// Used in startup
        /// </summary>
        private void RemoveIndexWriterLockFile(TextWriter consoleOut)
        {
            // delete write.lock if necessary
            var lockFilePath = IndexDirectory.IndexLockFilePath;
            if (lockFilePath == null)
                return;

            consoleOut?.WriteLine($"Index directory: {IndexDirectory.CurrentDirectory}");

            if (File.Exists(lockFilePath))
            {
                var endRetry = DateTime.UtcNow.AddSeconds(Configuration.Lucene29.LuceneLockDeleteRetryInterval);
                consoleOut?.WriteLine("Index directory is read only.");

                // retry write.lock for a given period of time
                while (true)
                {
                    try
                    {
                        File.Delete(lockFilePath);
                        consoleOut?.WriteLine("Index directory lock removed.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(5000);
                        if (DateTime.UtcNow > endRetry)
                            throw new IOException("Cannot remove the index lock: " + ex.Message, ex);
                    }
                }
            }
            else
            {
                consoleOut?.WriteLine("Index directory is read/write.");
            }
        }

        /* ============================================================================================= Document Operations */

        public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<DocumentUpdate> updates, IEnumerable<IndexDocument> additions)
        {
            using (var wrFrame = IndexWriterFrame.Get(_writer, _writerRestartLock, false)) // // AddTree
            {
                if (deletions != null)
                    wrFrame.IndexWriter.DeleteDocuments(GetTerms(deletions));

                if (updates != null)
                {
                    foreach (var update in updates)
                    {
                        if (update.Document != null)
                            wrFrame.IndexWriter.UpdateDocument(GetTerm(update.UpdateTerm), GetDocument(update.Document));
                    }
                }

                if (additions != null)
                {
                    foreach (var snDoc in additions)
                    {
                        if (snDoc != null)
                        {
                            // pessimistic approach: delete document before adding it to avoid duplicate index documents
                            wrFrame.IndexWriter.DeleteDocuments(GetVersionIdTerm(snDoc.VersionId));
                            wrFrame.IndexWriter.AddDocument(GetDocument(snDoc));
                        }
                    }
                }
            }
        }

        private Term GetTerm(SnTerm snTerm)
        {
            return GetTerms(snTerm).First();
        }
        private Term[] GetTerms(IEnumerable<SnTerm> snTerms)
        {
            List<Term> terms = new List<Term>();
            foreach (var snTerm in snTerms)
                terms.AddRange(GetTerms(snTerm));
            return terms.ToArray();
        }
        private Term[] GetTerms(SnTerm snTerm)
        {
            switch (snTerm.Type)
            {
                case IndexValueType.String:
                    return new[] { new Term(snTerm.Name, snTerm.StringValue) };
                case IndexValueType.StringArray:
                    return snTerm.StringArrayValue.Select(s => new Term(snTerm.Name, s)).ToArray();
                case IndexValueType.Bool:
                    return new[] { new Term(snTerm.Name, snTerm.BooleanValue ? IndexValue.Yes : IndexValue.No) };
                case IndexValueType.Int:
                    return new[] { new Term(snTerm.Name, NumericUtils.IntToPrefixCoded(snTerm.IntegerValue)) };
                case IndexValueType.Long:
                    return new[] { new Term(snTerm.Name, NumericUtils.LongToPrefixCoded(snTerm.LongValue)) };
                case IndexValueType.Float:
                    return new[] { new Term(snTerm.Name, NumericUtils.FloatToPrefixCoded(snTerm.SingleValue)) };
                case IndexValueType.Double:
                    return new[] { new Term(snTerm.Name, NumericUtils.DoubleToPrefixCoded(snTerm.DoubleValue)) };
                case IndexValueType.DateTime:
                    return new[] { new Term(snTerm.Name, NumericUtils.LongToPrefixCoded(snTerm.DateTimeValue.Ticks)) };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private Term GetVersionIdTerm(int versionId)
        {
            return new Term(IndexFieldName.VersionId, NumericUtils.IntToPrefixCoded(versionId));
        }

        private Document GetDocument(IndexDocument snDoc)
        {
            try
            {
                var doc = new Document();
                foreach (var indexField in snDoc)
                    AddFieldToDocument(indexField, doc);
                return doc;
            }
            catch (Exception e)
            {
                var path = snDoc.GetStringValue(IndexFieldName.Path) ?? string.Empty;
                var msg = "Error during indexing: the document data loaded from the database or the generated Lucene Document is invalid. " +
                          "Please save the content to regenerate the index for it. Path: " + path;
                SnLog.WriteException(e, msg);
                SnTrace.Index.WriteError(msg);
                SnTrace.Index.WriteError("LM: Error during indexing: " + e);

                throw;
            }

        }

        private void AddFieldToDocument(IndexField indexField, Document doc)
        {
            var name = indexField.Name;
            var store = EnumConverter.ToLuceneIndexStoringMode(indexField.Store);
            var mode = EnumConverter.ToLuceneIndexingMode(indexField.Mode);
            var termVect = EnumConverter.ToLuceneIndexTermVector(indexField.TermVector);
            switch (indexField.Type)
            {
                case IndexValueType.String:
                    doc.Add(new Field(name, indexField.StringValue, store, mode, termVect));
                    break;
                case IndexValueType.StringArray:
                    foreach (var item in indexField.StringArrayValue)
                        doc.Add(new Field(name, item, store, mode, termVect));
                    break;
                case IndexValueType.Bool:
                    doc.Add(new Field(name, indexField.BooleanValue ? IndexValue.Yes : IndexValue.No, store, mode, termVect));
                    break;
                case IndexValueType.Int:
                    doc.Add(new NumericField(name, store, indexField.Mode != IndexingMode.No).SetIntValue(indexField.IntegerValue));
                    break;
                case IndexValueType.Long:
                    doc.Add(new NumericField(name, store, indexField.Mode != IndexingMode.No).SetLongValue(indexField.LongValue));
                    break;
                case IndexValueType.Float:
                    doc.Add(new NumericField(name, store, indexField.Mode != IndexingMode.No).SetFloatValue(indexField.SingleValue));
                    break;
                case IndexValueType.Double:
                    doc.Add(new NumericField(name, store, indexField.Mode != IndexingMode.No).SetDoubleValue(indexField.DoubleValue));
                    break;
                case IndexValueType.DateTime:
                    doc.Add(new NumericField(name, store, indexField.Mode != IndexingMode.No).SetLongValue(indexField.DateTimeValue.Ticks));
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown SnTermType: {indexField.Type}");
            }
        }

        /* ============================================================================================= */

        private void Commit(bool reopenReader, IndexingActivityStatus state = null)
        {
            using (var op = SnTrace.Index.StartOperation("LM: Commit. reopenReader:{0}", reopenReader))
            {
                using (var wrFrame = IndexWriterFrame.Get(_writer, _writerRestartLock, !reopenReader)) // // Commit
                {
                    //TODO: CommitState: cleanup commitState if null if needed in the distributed environment.
                    //var commitState = state ?? IndexManager.GetCurrentIndexingActivityStatus();
                    var commitState = state;

                    var commitStateMessage = commitState?.ToString();

                    SnTrace.Index.Write("LM: Committing_writer. commitState: " + commitStateMessage);

                    // Write a fake document to make sure that the index changes are written to the file system.
                    wrFrame.IndexWriter.UpdateDocument(new Term(COMMITFIELDNAME, COMMITFIELDNAME), GetFakeDocument());

                    if (commitState != null)
                        wrFrame.IndexWriter.Commit(GetCommitUserData(commitState));
                    else
                        wrFrame.IndexWriter.Commit();

                    if (reopenReader)
                        ReopenReader();
                }

                op.Successful = true;
            }
        }

        private void ReopenReader()
        {
            using (var op = SnTrace.Index.StartOperation("LM: ReopenReader"))
            {
                var retry = 0;
                Exception e = null;
                while (retry++ < REOPENRETRYMAX)
                {
                    try
                    {
                        if (retry > 1)
                            SnTrace.Index.Write("LM: REOPEN READER {0}. ATTEMPT", retry);

                        _reader = _writer.GetReader();

#pragma warning disable 420
                        var recentlyUsedReaderFrames = Interlocked.Exchange(ref _recentlyUsedReaderFrames, 0);
#pragma warning restore 420

                        op.Successful = true;
                        IndexReopenedAt = DateTime.UtcNow;
                        SnTrace.Index.Write("Recently used reader frames from last reopening reader: {0}", recentlyUsedReaderFrames);
                        return;
                    }
                    catch (AlreadyClosedException ace)
                    {
                        e = ace;
                        Thread.Sleep(100);
                    }
                }
                if (e != null)
                    throw new ApplicationException(String.Concat("Indexwriter is closed after ", retry, " attempt."), e);
            }
        }

        /* ============================================================================= IndexingActivityStatus */

        private const string LastActivityIdKey = "LastActivityId";
        private const string GapsKey = "Gaps";
        private static Dictionary<string, string> GetCommitUserData(IndexingActivityStatus status)
        {
            var result = new Dictionary<string, string> { { LastActivityIdKey, status.LastActivityId.ToString() } };
            var gaps = status.Gaps;
            if (gaps != null && gaps.Length > 0)
                result.Add(GapsKey, string.Join(",", gaps));
            return result;
        }
        private static IndexingActivityStatus ParseFromReader(IndexReader reader)
        {
            var commitUserData = reader.GetCommitUserData();
            var result = new IndexingActivityStatus();
            if (commitUserData != null)
            {
                string value;

                int lastActivityId;
                if (commitUserData.TryGetValue(LastActivityIdKey, out value))
                    if (!string.IsNullOrEmpty(value))
                        if (int.TryParse(value, out lastActivityId))
                            result.LastActivityId = lastActivityId;

                if (commitUserData.TryGetValue(GapsKey, out value))
                    if (!string.IsNullOrEmpty(value))
                        result.Gaps = value
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => { int i; return int.TryParse(s, out i) ? i : 0; })
                            .Where(i => i > 0)
                            .ToArray();
            }
            return result;
        }

        /* ==================================================================================================== */

        private const string COMMITFIELDNAME = "$#COMMIT";
        private const string COMMITDATAFIELDNAME = "$#DATA";
        private const int REOPENRETRYMAX = 2;

        private IndexWriter _writer;
        private IndexReader _reader;
        private readonly object _commitLock = new object();

        private readonly ReaderWriterLockSlim _writerRestartLock = new ReaderWriterLockSlim();
        private volatile int _recentlyUsedReaderFrames;

        public IndexReaderFrame GetIndexReaderFrame(bool dirty = false)
        {
            var needReopen = (DateTime.UtcNow - IndexReopenedAt) > ForceReopenFrequency;
            if (!dirty || needReopen)
                if (!_reader.IsCurrent())
                    using (IndexWriterFrame.Get(_writer, _writerRestartLock, false))
                        ReopenReader();

#pragma warning disable 420
            Interlocked.Increment(ref _recentlyUsedReaderFrames);
#pragma warning restore 420

            return new IndexReaderFrame(_reader);
        }

        private void CreateWriterAndReader()
        {
            var path = IndexDirectory.CurrentDirectory;
            var directory = FSDirectory.Open(new DirectoryInfo(path));
            EnsureIndex(path);

            _writer = new IndexWriter(directory, GetAnalyzer(), false, IndexWriter.MaxFieldLength.LIMITED);

            _writer.SetMaxMergeDocs(Configuration.Lucene29.LuceneMaxMergeDocs);
            _writer.SetMergeFactor(Configuration.Lucene29.LuceneMergeFactor);
            _writer.SetRAMBufferSizeMB(Configuration.Lucene29.LuceneRAMBufferSizeMB);
            _reader = _writer.GetReader();
        }
        private void EnsureIndex(string path)
        {
            // new IndexWriter(createNew = false) cannot be created if the directory is empty
            if (System.IO.Directory.GetFiles(path).Any())
                return;
            var dir = FSDirectory.Open(new DirectoryInfo(IndexDirectory.CurrentDirectory));
            var writer = new IndexWriter(dir, GetAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
            writer.Commit();
            writer.Close();
        }

        public Analyzer GetAnalyzer()
        {
            return new SnPerFieldAnalyzerWrapper(AnalyzerInfo);
        }

        public void SetIndexingInfo(IDictionary<string, Analyzer> analyzerInfo, IDictionary<string, IndexValueType> indexFieldTypeInfo)
        {
            AnalyzerInfo = analyzerInfo;
            IndexFieldTypeInfo = indexFieldTypeInfo;
        }

        private Document GetFakeDocument()
        {
            var value = Guid.NewGuid().ToString();
            var doc = new Document();
            doc.Add(new Field(COMMITFIELDNAME, COMMITFIELDNAME, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
            doc.Add(new Field(COMMITDATAFIELDNAME, value, Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));

            return doc;
        }
    }
}

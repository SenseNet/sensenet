﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using System.Configuration;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage.Diagnostics;
using SenseNet.TaskManagement.Core;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Security;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Represents a running Repository. There is always one instance in any appdomain.
    /// Repository will be stopped when the instance is disposing.
    /// </summary>
    public sealed class RepositoryInstance : IDisposable
    {
        /// <summary>
        /// Provides some information about the boot sequence
        /// </summary>
        public class StartupInfo
        {
            /// <summary>
            /// Name of the assemblies thats are loaded before startup sequence begins.
            /// </summary>
            public string[] AssembliesBeforeStart { get; internal set; }
            /// <summary>
            /// Name of the assemblies thats are loaded from the appdomain's working directory.
            /// </summary>
            public string[] ReferencedAssemblies { get; internal set; }
            /// <summary>
            /// Name of the assemblies thats are loaded from an additional path (if there is).
            /// </summary>
            public string[] Plugins { get; internal set; }
            /// <summary>
            /// Absolute path of the index directory.
            /// </summary>
            public string IndexDirectory { get; internal set; }
            /// <summary>
            /// True if the index was read only before startup. Means: there was writer.lock file in the configured index directory.
            /// </summary>
            public bool IndexWasReadOnly { get; internal set; }
            /// <summary>
            /// Moment of the start before executing the startup sequence.
            /// </summary>
            public DateTime Starting { get; internal set; }
            /// <summary>
            /// Moment of the start after executing the startup sequence.
            /// </summary>
            public DateTime Started { get; internal set; }
        }

        private StartupInfo _startupInfo;
        private RepositoryStartSettings.ImmutableRepositoryStartSettings _settings;
        private static RepositoryInstance _instance;
        private static object _startupSync = new Object();
        private static object _shutDownSync = new Object();

        /// <summary>
        /// Gets a <see cref="StartupInfo"/> instance that provides some information about the boot sequence.
        /// </summary>
        public StartupInfo StartupTrace { get { return _startupInfo; } }
        /// <summary>
        /// Gets the startup control information.
        /// </summary>
        [Obsolete("Use individual immutable properties instead.")]
        public RepositoryStartSettings.ImmutableRepositoryStartSettings StartSettings => _settings;

        /// <summary>
        /// Gets the started up instance or null.
        /// </summary>
        public static RepositoryInstance Instance { get { return _instance; } }

        public TextWriter Console => _settings?.Console;
        public bool BackupIndexAtTheEnd => _settings?.BackupIndexAtTheEnd ?? false;

        private RepositoryInstance()
        {
            _startupInfo = new StartupInfo { Starting = DateTime.UtcNow };
        }

        private static bool _started;
        internal static RepositoryInstance Start(RepositoryStartSettings settings)
        {
            if (!_started)
            {
                lock (_startupSync)
                {
                    if (!_started)
                    {
                        var instance = new RepositoryInstance();
                        instance._settings = new RepositoryStartSettings.ImmutableRepositoryStartSettings(settings);
                        _instance = instance;
                        try
                        {
                            instance.DoStart();
                        }
                        catch (Exception)
                        {
                            _instance = null;
                            throw;
                        }
                        _started = true;
                    }
                }
            }
            return _instance;
        }
        internal void DoStart()
        {
            ConsoleWriteLine();
            ConsoleWriteLine("Starting Repository...");
            ConsoleWriteLine();

            LoggingSettings.SnTraceConfigurator.UpdateStartupCategories();
            
            TypeHandler.Initialize(_settings.Providers);

            InitializeLogger();

            // Lucene subsystem behaves strangely if the enums are not initialized.
            var x = Lucene.Net.Documents.Field.Index.NO;
            var y = Lucene.Net.Documents.Field.Store.NO;
            var z = Lucene.Net.Documents.Field.TermVector.NO;

            CounterManager.Start();

            RegisterAppdomainEventHandlers();

            if (_settings.IndexPath != null)
                StorageContext.Search.SetIndexDirectoryPath(_settings.IndexPath);
            RemoveIndexWriterLockFile();
            _startupInfo.IndexDirectory = System.IO.Path.GetDirectoryName(StorageContext.Search.IndexLockFilePath);

            LoadAssemblies();

            SenseNet.ContentRepository.Storage.Security.SecurityHandler.StartSecurity(_settings.IsWebContext);

            using (new SenseNet.ContentRepository.Storage.Security.SystemAccount())
                StartManagers();

            LoggingSettings.SnTraceConfigurator.UpdateCategories();

            InitializeOAuthProviders();

            ConsoleWriteLine();
            ConsoleWriteLine("Repository has started.");
            ConsoleWriteLine();

            _startupInfo.Started = DateTime.UtcNow;
        }
        /// <summary>
        /// Starts Lucene if it is not running.
        /// </summary>
        public void StartLucene()
        {
            if (LuceneManagerIsRunning)
            {
                ConsoleWrite("LuceneManager has already started.");
                return;
            }
            ConsoleWriteLine("Starting LuceneManager:");

            SenseNet.Search.Indexing.LuceneManager.Start(_settings.Console);

            ConsoleWriteLine("LuceneManager has started.");
        }
        /// <summary>
        /// Starts workflow engine if it is not running.
        /// </summary>
        public void StartWorkflowEngine()
        {
            if (_workflowEngineIsRunning)
            {
                ConsoleWrite("Workflow engine has already started.");
                return;
            }
            ConsoleWrite("Starting Workflow subsystem ... ");
            var t = TypeResolver.GetType("SenseNet.Workflow.InstanceManager", false);
            if (t != null)
            {
                var m = t.GetMethod("StartWorkflowSystem", BindingFlags.Static | BindingFlags.Public);
                m.Invoke(null, new object[0]);
                _workflowEngineIsRunning = true;
                ConsoleWriteLine("ok.");
            }
            else
            {
                ConsoleWriteLine("NOT STARTED");
            }
        }

        private void LoadAssemblies()
        {
            string[] asmNames;
            _startupInfo.AssembliesBeforeStart = GetLoadedAsmNames().ToArray();
            var localBin = AppDomain.CurrentDomain.BaseDirectory;
            var pluginsPath = _settings.PluginsPath ?? localBin;

            if (System.Web.HttpContext.Current != null)
            {
                ConsoleWrite("Getting referenced assemblies ... ");
                System.Web.Compilation.BuildManager.GetReferencedAssemblies();
                ConsoleWriteLine("Ok.");
            }
            else
            {
                ConsoleWriteLine("Loading Assemblies from ", localBin, ":");
                asmNames = TypeResolver.LoadAssembliesFrom(localBin);
                foreach (string name in asmNames)
                    ConsoleWriteLine("  ", name);
            }
            _startupInfo.ReferencedAssemblies = GetLoadedAsmNames().Except(_startupInfo.AssembliesBeforeStart).ToArray();


            ConsoleWriteLine("Loading Assemblies from ", pluginsPath, ":");
            asmNames = TypeResolver.LoadAssembliesFrom(pluginsPath);
            _startupInfo.Plugins = GetLoadedAsmNames().Except(_startupInfo.AssembliesBeforeStart).Except(_startupInfo.ReferencedAssemblies).ToArray();

            if (_settings.Console == null)
                return;

            foreach (string name in asmNames)
                ConsoleWriteLine("  ", name);
            ConsoleWriteLine("Ok.");
            ConsoleWriteLine();
        }
        private IEnumerable<string> GetLoadedAsmNames()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName).ToArray();
        }
        private void StartManagers()
        {
            object dummy;
            SenseNet.Communication.Messaging.IClusterChannel channel = null;

            try
            {
                ConsoleWrite("Initializing cache ... ");
                dummy = SenseNet.ContentRepository.DistributedApplication.Cache.Count;
                ConsoleWriteLine("ok.");

                ConsoleWrite("Starting message channel ... ");
                channel = SenseNet.ContentRepository.DistributedApplication.ClusterChannel;
                ConsoleWriteLine("ok.");

                ConsoleWrite("Sending greeting message ... ");
                (new PingMessage(new string[0])).Send();
                ConsoleWriteLine("ok.");

                ConsoleWrite("Starting NodeType system ... ");
                dummy = ActiveSchema.NodeTypes[0];
                ConsoleWriteLine("ok.");

                ConsoleWrite("Starting ContentType system ... ");
                dummy = SenseNet.ContentRepository.Schema.ContentType.GetByName("GenericContent");
                ConsoleWriteLine("ok.");

                ConsoleWrite("Starting AccessProvider ... ");
                dummy = User.Current;
                ConsoleWriteLine("ok.");

                if (_settings.StartLuceneManager)
                    StartLucene();
                else
                    ConsoleWriteLine("LuceneManager is not started.");

                // switch on message processing after LuceneManager was started
                channel.AllowMessageProcessing = true;

                SenseNet.Search.Indexing.IndexHealthMonitor.Start(_settings.Console);

                if (_settings.StartWorkflowEngine)
                    StartWorkflowEngine();
                else
                    ConsoleWriteLine("Workflow subsystem is not started.");

                ConsoleWrite("Loading string resources ... ");
                dummy = SenseNetResourceManager.Current;
                ConsoleWriteLine("ok.");

                serviceInstances = new List<ISnService>();
                foreach (var serviceType in TypeResolver.GetTypesByInterface(typeof(ISnService)))
                {
                    var service = (ISnService)Activator.CreateInstance(serviceType);
                    service.Start();
                    ConsoleWriteLine("Service started: ", serviceType.Name);
                    serviceInstances.Add(service);
                }

                // register this application in the task management component
                SnTaskManager.RegisterApplication();
            }
            catch
            {
                // If an error occoured, shut down the cluster channel.
                if (channel != null)
                    channel.ShutDown();

                throw;
            }
        }

        private List<ISnService> serviceInstances;

        private static void InitializeOAuthProviders()
        {
            var providerTypeNames = new List<string>();

            foreach (var providerType in TypeResolver.GetTypesByBaseType(typeof(OAuthProvider)).Where(t => !t.IsAbstract))
            {
                var provider = TypeResolver.CreateInstance(providerType.FullName) as OAuthProvider;
                if (provider == null)
                    continue;

                if (string.IsNullOrEmpty(provider.ProviderName))
                {
                    SnLog.WriteWarning($"OAuth provider type {providerType.FullName} does not expose a valid ProviderName value, therefore cannot be initialized.");
                    continue;
                }
                if (string.IsNullOrEmpty(provider.IdentifierFieldName))
                {
                    SnLog.WriteWarning($"OAuth provider type {providerType.FullName} does not expose a valid IdentifierFieldName value, therefore cannot be initialized.");
                    continue;
                }

                Providers.Instance.SetProvider(provider.GetProviderRegistrationName(), provider);
                providerTypeNames.Add($"{providerType.FullName} ({provider.ProviderName})");
            }

            if (providerTypeNames.Any())
            {
                SnLog.WriteInformation("OAuth providers registered: " + Environment.NewLine +
                                       string.Join(Environment.NewLine, providerTypeNames));
            }
        }

        private static void InitializeLogger()
        {
            var logSection = ConfigurationManager.GetSection("loggingConfiguration");
            if (logSection != null)
                SnLog.Instance = new EntLibLoggerAdapter();
            else
                SnLog.Instance = new DebugWriteLoggerAdapter();
        }

        private void RemoveIndexWriterLockFile()
        {
            // delete write.lock if necessary
            var lockFilePath = StorageContext.Search.IndexLockFilePath;
            if (lockFilePath == null)
                return;
            if (System.IO.File.Exists(lockFilePath))
            {
                _startupInfo.IndexWasReadOnly = true;
                var endRetry = DateTime.UtcNow.AddSeconds(Indexing.LuceneLockDeleteRetryInterval);

                // retry write.lock for a given period of time
                while (true)
                {
                    try
                    {
                        System.IO.File.Delete(lockFilePath);
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(5000);
                        if (DateTime.UtcNow > endRetry)
                            throw new System.IO.IOException("Cannot remove the index lock: " + ex.Message, ex);
                    }
                }
            }
            else
            {
                _startupInfo.IndexWasReadOnly = false;
                ConsoleWriteLine("Index directory is read/write.");
            }
        }
        private void RegisterAppdomainEventHandlers()
        {
            AppDomain appDomain = AppDomain.CurrentDomain;
            appDomain.UnhandledException += new UnhandledExceptionEventHandler(Domain_UnhandledException);
        }

        private void Domain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e?.ExceptionObject as Exception;
            if(ex != null)
                SnLog.WriteException(ex, "Domain_UnhandledException", EventId.NotDefined);
            else
                SnLog.WriteError("Domain_UnhandledException. ExceptionObject is " + e?.ExceptionObject ?? "null", EventId.NotDefined);
        }
        private Assembly Domain_TypeResolve(object sender, ResolveEventArgs args)
        {
            SnTrace.System.Write("Domain_TypeResolve: " + args.Name);
            return null;
        }
        private Assembly Domain_ResourceResolve(object sender, ResolveEventArgs args)
        {
            SnTrace.System.Write("Domain_ResourceResolve: " + args.Name);
            return null;
        }
        private Assembly Domain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            SnTrace.System.Write("Domain_ReflectionOnlyAssemblyResolve: " + args.Name);
            return null;
        }
        private Assembly Domain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            SnTrace.System.Write("Domain_AssemblyResolve: " + args.Name);
            return null;
        }
        private void Domain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            SnTrace.System.Write("Domain_AssemblyLoad: " + args.LoadedAssembly.FullName);
        }

        internal static void Shutdown()
        {
            if (_instance == null)
            {
                SnLog.WriteWarning("Repository shutdown has already completed.");
                return;
            }

            lock (_shutDownSync)
            {
                if (_instance == null)
                {
                    SnLog.WriteWarning("Repository shutdown has already completed.");
                    return;
                }

                SnTrace.Repository.Write("Sending a goodbye message.");

                _instance.ConsoleWriteLine();

                _instance.ConsoleWriteLine("Sending a goodbye message...");
                DistributedApplication.ClusterChannel.ClusterMemberInfo.NeedToRecover = false;
                var pingMessage = new PingMessage();
                pingMessage.Send();

                foreach (var svc in _instance.serviceInstances)
                {
                    SnTrace.Repository.Write("Shutting down {0}", svc.GetType().Name);
                    svc.Shutdown();
                }

                SnTrace.Repository.Write("Shutting down {0}", DistributedApplication.ClusterChannel.GetType().Name);
                DistributedApplication.ClusterChannel.ShutDown();

                if (Instance.BackupIndexAtTheEnd)
                {
                    SnTrace.Repository.Write("Backing up the index.");
                    if (LuceneManagerIsRunning)
                    {
                        _instance.ConsoleWriteLine("Backing up the index...");
                        SenseNet.Search.Indexing.BackupTools.SynchronousBackupIndex();
                        _instance.ConsoleWriteLine("The backup of index is finished.");
                    }
                    else
                    {
                        _instance.ConsoleWriteLine("Backing up index is skipped because Lucene was not started.");
                    }
                }

                if (LuceneManagerIsRunning)
                {
                    SnTrace.Repository.Write("Shutting down LuceneManager.");
                    SenseNet.Search.Indexing.LuceneManager.ShutDown();
                }

                SnTrace.Repository.Write("Waiting for writer lock file is released.");
                WaitForWriterLockFileIsReleased(WaitForLockFileType.OnEnd);

                var t = DateTime.UtcNow - _instance._startupInfo.Starting;
                var msg = String.Format("Repository has stopped. Running time: {0}.{1:d2}:{2:d2}:{3:d2}", t.Days,
                                        t.Hours, t.Minutes, t.Seconds);

                SnTrace.Repository.Write(msg);
                SnTrace.Flush();

                _instance.ConsoleWriteLine(msg);
                _instance.ConsoleWriteLine();
                SnLog.WriteInformation(msg);
                _instance = null;
            }
        }

        public void ConsoleWrite(params string[] text)
        {
            if (_settings.Console == null)
                return;
            foreach (var s in text)
                _settings.Console.Write(s);
        }
        public void ConsoleWriteLine(params string[] text)
        {
            if (_settings.Console == null)
                return;
            ConsoleWrite(text);
            _settings.Console.WriteLine();
        }

        internal static bool Started()
        {
            return _started;
        }

        // ======================================== Wait for write.lock
        private const string WAITINGFORLOCKSTR = "write.lock exists, waiting for removal...";
        private const string WRITELOCKREMOVEERRORSUBJECTSTR = "Error at application start";
        private const string WRITELOCKREMOVEERRORTEMPLATESTR = "Write.lock was present at application start and was not removed within set timeout interval ({0} seconds) - a previous appdomain may use the index. Write.lock deletion and application start is forced. AppDomain friendlyname: {1}, base directory: {2}";
        private const string WRITELOCKREMOVEERRORONENDTEMPLATESTR = "Write.lock was present at shutdown and was not removed within set timeout interval ({0} seconds) - application exit is forced. AppDomain friendlyname: {1}, base directory: {2}";
        private const string WRITELOCKREMOVEEMAILERRORSTR = "Could not send notification email about write.lock removal. Check the notification section in the config file!";
        public enum WaitForLockFileType { OnStart = 0, OnEnd }
        /// <summary>
        /// Waits for releasing index writer lock file in the configured index directory. Timeout: configured with IndexLockFileWaitForRemovedTimeout key.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public static bool WaitForWriterLockFileIsReleased()
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
            return WaitForWriterLockFileIsReleased(indexDirectory, Indexing.IndexLockFileWaitForRemovedTimeout);
        }
        /// <summary>
        /// Waits for releasing index writer lock file in the specified directory and timeout.
        /// Returns true if the lock was released. Returns false if the time has expired.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <returns>Returns true if the lock was released. Returns false if the time has expired.</returns>
        public static bool WaitForWriterLockFileIsReleased(string indexDirectory, int timeout)
        {
            if (indexDirectory == null)
            {
                SnTrace.Repository.Write("Index directory not found.");
                return true;
            }

            var lockFilePath = System.IO.Path.Combine(indexDirectory, Lucene.Net.Index.IndexWriter.WRITE_LOCK_NAME);
            var deadline = DateTime.UtcNow.AddSeconds(timeout);

            SnTrace.Repository.Write("Waiting for lock file to disappear: " + lockFilePath);

            while (System.IO.File.Exists(lockFilePath))
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
        /// Waits for write.lock to disappear for a configured time interval. Timeout: configured with IndexLockFileWaitForRemovedTimeout key. 
        /// If timeout is exceeded an error is logged and execution continues. For errors at OnStart an email is also sent to a configured address.
        /// </summary>
        /// <param name="waitType">A parameter that influences the logged error message and email template only.</param>
        public static void WaitForWriterLockFileIsReleased(WaitForLockFileType waitType)
        {
            // check if writer.lock is still there -> if yes, wait for other appdomain to quit or lock to disappear - until a given timeout.
            // after timeout is passed, Repository.Start will deliberately attempt to remove lock file on following startup

            if (!WaitForWriterLockFileIsReleased())
            {
                // lock file was not removed by other or current appdomain for the given time interval (onstart: other appdomain might use it, onend: current appdomain did not release it yet)
                // onstart -> notify operator and start repository anyway
                // onend -> log error, and continue
                var template = waitType == WaitForLockFileType.OnEnd ? WRITELOCKREMOVEERRORONENDTEMPLATESTR : WRITELOCKREMOVEERRORTEMPLATESTR;
                SnLog.WriteError(string.Format(template, Indexing.IndexLockFileWaitForRemovedTimeout,
                    AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.BaseDirectory));

                if (waitType == WaitForLockFileType.OnStart)
                    RepositoryInstance.SendWaitForLockErrorMail();
            }
        }
        private static void SendWaitForLockErrorMail()
        {
            if (!string.IsNullOrEmpty(Notification.NotificationSender) && !string.IsNullOrEmpty(Indexing.IndexLockFileRemovedNotificationEmail))
            {
                try
                {
                    var smtpClient = new System.Net.Mail.SmtpClient();
                    var msgstr = string.Format(WRITELOCKREMOVEERRORTEMPLATESTR,
                        Indexing.IndexLockFileWaitForRemovedTimeout,
                        AppDomain.CurrentDomain.FriendlyName,
                        AppDomain.CurrentDomain.BaseDirectory);
                    var msg = new System.Net.Mail.MailMessage(
                        Notification.NotificationSender,
                        Indexing.IndexLockFileRemovedNotificationEmail.Replace(';', ','),
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

        // ========================================

        private bool _workflowEngineIsRunning;

        // ======================================== LuceneManager hooks

        public static bool LuceneManagerIsRunning
        {
            get
            {
                if (_instance == null)
                    throw new NotSupportedException("Querying running state of LuceneManager is not supported when RepositoryInstance is not created.");
                return SenseNet.Search.Indexing.LuceneManager.Running;
            }
        }
        public static bool IndexingPaused
        {
            get
            {
                if (_instance == null)
                    throw new NotSupportedException("Querying pausing state of LuceneManager is not supported when RepositoryInstance is not created.");
                return SenseNet.Search.Indexing.LuceneManager.Paused;
            }
        }

        internal static bool RestoreIndexOnStartup()
        {
            if (_instance == null)
                return true;
            return _instance._settings.RestoreIndex;
        }

        // ======================================== Outer search engine

        public static bool ContentQueryIsAllowed
        {
            get
            {
                return StorageContext.Search.IsOuterEngineEnabled &&
                       StorageContext.Search.SearchEngine != InternalSearchEngine.Instance &&
                       RepositoryInstance.LuceneManagerIsRunning;
            }
        }

        // ======================================== IDisposable
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!this._disposed)
                if (disposing)
                    Shutdown();
            _disposed = true;
        }
        ~RepositoryInstance()
        {
            Dispose(false);
        }

    }
}

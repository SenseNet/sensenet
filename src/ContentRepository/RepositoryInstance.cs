using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using System.Reflection;
using System.Runtime.ExceptionServices;
using SenseNet.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Communication.Messaging;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search.Querying;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;
using EventId = SenseNet.Diagnostics.EventId;

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
        private static object _startStopSync = new object();

        /// <summary>
        /// Gets a <see cref="StartupInfo"/> instance that provides some information about the boot sequence.
        /// </summary>
        public StartupInfo StartupTrace { get { return _startupInfo; } }
        /// <summary>
        /// Gets the startup control information.
        /// </summary>
        [Obsolete("Use individual immutable properties instead.", true)]
        public RepositoryStartSettings.ImmutableRepositoryStartSettings StartSettings => _settings;

        /// <summary>
        /// Gets the started up instance or null.
        /// </summary>
        public static RepositoryInstance Instance { get { return _instance; } }
        
        public TextWriter Console => _settings?.Console;

        private RepositoryInstance()
        {
            _startupInfo = new StartupInfo { Starting = DateTime.UtcNow };
        }

        private ILogger<RepositoryInstance> _logger;

        // True after the subsystems have started; the outer Repository.Start may still
        // be initializing the repository. Reset whenever the instance is shut down.
        private static bool _started;

        // True only after the complete Repository.Start succeeds. A startup or shutdown
        // failure invalidates the environment. A clean shutdown preserves this flag so
        // tests can reuse the installed data and providers, but not the disposed instance.
        private static bool _isReusable;

        internal static bool IsReusable => _isReusable;
        internal static void InvalidateReuse() => _isReusable = false;

        // Installation can call Repository.Start recursively, including across async callbacks.
        private static readonly AsyncLocal<int> _startupDepth = new AsyncLocal<int>();

        internal static RepositoryInstance RunStartup(Func<RepositoryInstance> initialize)
        {
            _isReusable = false;
            _startupDepth.Value++;
            try
            {
                var instance = initialize();
                if (_startupDepth.Value == 1)
                    _isReusable = true;
                return instance;
            }
            catch (Exception startupError)
            {
                try
                {
                    Shutdown();
                }
                catch (Exception shutdownError)
                {
                    // Keep the startup failure as the primary exception.
                    startupError.Data["RepositoryShutdownException"] = shutdownError;
                }
                finally
                {
                    _isReusable = false;
                }
                throw;
            }
            finally
            {
                _startupDepth.Value--;
            }
        }

        internal static RepositoryInstance Start(RepositoryStartSettings settings)
        {
            if (!_started)
            {
                lock (_startStopSync)
                {
                    if (!_started)
                    {
                        var instance = new RepositoryInstance();
                        instance._settings = new RepositoryStartSettings.ImmutableRepositoryStartSettings(settings);
                        _instance = instance;
                        _instance._logger = instance._settings.Services.GetService<ILogger<RepositoryInstance>>();
                        Providers.Instance.RepositoryStatus?.SetStatus("Starting subsystems");
                        instance.DoStart();
                        Providers.Instance.RepositoryStatus?.SetStatus("Subsystems started");
                        _started = true;
                    }
                }
            }
            return _instance;
        }
        internal void DoStart()
        {
            ConsoleWriteLine(false);
            ConsoleWriteLine(true, "Starting Repository.");
            ConsoleWriteLine(false);

            LoggingSettings.SnTraceConfigurator.ConfigureCategories();
            if (_settings.TraceCategories != null)
                LoggingSettings.SnTraceConfigurator.UpdateCategories(_settings.TraceCategories);

            Providers.Instance.RepositoryStatus?.SetStatus("  Initializing Logger");
            InitializeLogger();

            RegisterAppdomainEventHandlers();

            if (_settings.IndexPath != null)
                Providers.Instance.SearchManager.IndexDirectoryPath = _settings.IndexPath;

            LoadAssemblies();

            Providers.Instance.RepositoryStatus?.SetStatus("  Starting Security");
            var securityHandler = _settings.Services.GetRequiredService<SecurityHandler>();
            _shutdownActions.Push(("Shutting down Security.", securityHandler.ShutDownSecurity));
            securityHandler.StartSecurity(_settings.Services);

            //UNDONE: modernize TemplateManager
            // Set legacy collection from the new services collection and reset the
            // current singleton list to force the system to regenerate it. 
            TemplateManager.TemplateReplacerInstances =
                _settings.Services.GetService<IEnumerable<TemplateReplacerBase>>()?.ToArray() ??
                Array.Empty<TemplateReplacerBase>();
            TemplateManager.Clear();

            SnQueryVisitor.VisitorExtensionTypes = new[] {typeof(Sharing.SharingVisitor)};

            // We have to log the access provider here because it cannot be logged 
            // during creation as it would lead to a circular reference.
            _logger.LogInformation($"AccessProvider created: {AccessProvider.Current?.GetType().FullName}");
            using (new SystemAccount())
                StartManagers();

            LoggingSettings.SnTraceConfigurator.UpdateCategoriesBySettings();
            
            ConsoleWriteLine(false);
            ConsoleWriteLine(true, "Repository has started.");
            ConsoleWriteLine(false);

            _startupInfo.Started = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts IndexingEngine if it is not running.
        /// </summary>
        public void StartIndexingEngine()
        {
            if (IndexingEngineIsRunning)
            {
                ConsoleWrite(true, "IndexingEngine has already started.");
                return;
            }
            Providers.Instance.RepositoryStatus?.SetStatus("  Starting IndexingEngine");
            ConsoleWriteLine(true, "Starting IndexingEngine.");
            var indexManager = Providers.Instance.IndexManager;
            _shutdownActions.Push(("Shutting down IndexingEngine.", indexManager.ShutDown));
            indexManager.StartAsync(_settings.Console, CancellationToken.None).GetAwaiter().GetResult();
            Providers.Instance.SearchManager.SearchEngine.SetIndexingInfo(ContentTypeManager.Instance.IndexingInfo);
            ConsoleWriteLine(true, "IndexingEngine has started.");
        }

        /// <summary>
        /// Starts workflow engine if it is not running.
        /// </summary>
        [Obsolete("Do not use anymore this method.", true)]
        public void StartWorkflowEngine() { }

        private void LoadAssemblies()
        {
            _startupInfo.AssembliesBeforeStart = GetLoadedAsmNames().ToArray();
            var localBin = AppDomain.CurrentDomain.BaseDirectory;
            var pluginsPath = _settings.PluginsPath ?? localBin;

            ConsoleWriteLine(false, "Loading Assemblies from ", localBin, ":");
            var asmNames = TypeResolver.LoadAssembliesFrom(localBin);
            foreach (string name in asmNames)
                ConsoleWriteLine(false, "  ", name);

            _startupInfo.ReferencedAssemblies = GetLoadedAsmNames().Except(_startupInfo.AssembliesBeforeStart).ToArray();

            if (string.IsNullOrEmpty(pluginsPath) || pluginsPath.Equals(localBin, StringComparison.OrdinalIgnoreCase))
            {
                _startupInfo.Plugins = Array.Empty<string>();
                return;
            }

            ConsoleWriteLine(false, "Loading Assemblies from ", pluginsPath, ":");
            asmNames = TypeResolver.LoadAssembliesFrom(pluginsPath);
            _startupInfo.Plugins = GetLoadedAsmNames().Except(_startupInfo.AssembliesBeforeStart).Except(_startupInfo.ReferencedAssemblies).ToArray();

            if (_settings.Console == null)
                return;

            foreach (string name in asmNames)
                ConsoleWriteLine(false, "  ", name);
            ConsoleWriteLine(false, "Ok.");
            ConsoleWriteLine(false);
        }
        private IEnumerable<string> GetLoadedAsmNames()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName).ToArray();
        }
        private void StartManagers()
        {
            object dummy;
            IClusterChannel channel = null;

            try
            {
                ConsoleWrite(false, "Initializing cache ... ");
                dummy = Cache.Count;
                
                // Log this, because logging is switched off when creating the cache provider
                // to avoid circular reference.
                _logger.LogInformation($"CacheProvider created: {Cache.Instance?.GetType().FullName}");
                ConsoleWriteLine(false, "ok.");
                SnTrace.System.Write("Cache initialized.");

                Providers.Instance.RepositoryStatus?.SetStatus("  Initializing message channel");
                ConsoleWrite(false, "Starting message channel ... ");
                channel = Providers.Instance.ClusterChannelProvider;
                _shutdownActions.Push(($"Shutting down {channel.GetType().Name}.",
                    () => channel.ShutDownAsync(CancellationToken.None).GetAwaiter().GetResult()));
                channel.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
                ConsoleWriteLine(false, "ok.");
                _logger.LogInformation($"Message channel started:  {channel.GetType().FullName}. " +
                                       $"Instance id: {channel.ClusterMemberInfo.InstanceID}");

                ConsoleWrite(false, "Sending greeting message ... ");
                new PingMessage(new string[0]).SendAsync(CancellationToken.None).GetAwaiter().GetResult();
                ConsoleWriteLine(false, "ok.");
                SnTrace.System.Write("Greeting message sent (PingMessage).");

                Providers.Instance.RepositoryStatus?.SetStatus("  Initializing content type system");
                ConsoleWrite(false, "Starting NodeType system ... ");
                dummy = Providers.Instance.StorageSchema.NodeTypes[0];
                ConsoleWriteLine(false, "ok.");
                SnTrace.System.Write("NodeType system started.");

                ConsoleWrite(false, "Starting ContentType system ... ");
                dummy = ContentType.GetByName("GenericContent");
                ConsoleWriteLine(false, "ok.");
                SnTrace.System.Write("ContentType system started.");

                ConsoleWrite(false, "Starting AccessProvider ... ");
                dummy = User.Current;
                ConsoleWriteLine(false, "ok.");
                SnTrace.System.Write("AccessProvider started.");

                SnQuery.SetPermissionFilterFactory(Providers.Instance.PermissionFilterFactory);

                if (_settings.StartIndexingEngine)
                    StartIndexingEngine();
                else
                    ConsoleWriteLine(true, "IndexingEngine is not started.");

                // switch on message processing after IndexingEngine was started.
                channel.AllowMessageProcessing = true;

                ConsoleWrite(false, "Loading string resources ... ");
                dummy = SenseNetResourceManager.Current;
                ConsoleWriteLine(false, "ok.");
                SnTrace.System.Write("String resources loaded.");

                foreach (var service in _settings.Services.GetServices<ISnService>())
                {
                    Providers.Instance.RepositoryStatus?.SetStatus("  Starting service: " + service.GetType().FullName);
                    _shutdownActions.Push(($"Shutting down {service.GetType().Name}.", service.Shutdown));
                    service.Start();
                    ConsoleWriteLine(true, "Service started: " + service.GetType().Name);
                }

                // register this application in the task management component
                var taskManager = _settings.Services.GetRequiredService<ITaskManager>();
                taskManager.RegisterApplicationAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch
            {
                Providers.Instance.RepositoryStatus?.SetStatus("  Shutting down because of an error");
                throw;
            }
        }

        // Register cleanup before each start attempt so partial starts can be unwound too.
        private readonly Stack<(string Name, Action Action)> _shutdownActions =
            new Stack<(string Name, Action Action)>();

        private static void InitializeLogger()
        {
            // look for the configured logger
            SnLog.Instance = Providers.Instance.EventLogger ?? new DebugWriteLoggerAdapter();
            SnLog.AuditEventWriter = Providers.Instance.AuditEventWriter ?? throw new ApplicationException("Missing AuditEventWriter service.");

            //set configured tracers
            var tracers = Providers.Instance.Services.GetServices<ISnTracer>().ToArray();
            if (tracers.Length > 0)
            {
                SnTrace.SnTracers.Clear();
                SnTrace.SnTracers.AddRange(tracers);
            }

            SnTrace.System.Write("Loggers and tracers initialized. " +
                                     $"Loggers: {SnLog.Instance?.GetType().Name}. " +
                                     $"Tracers: {string.Join(", ", SnTrace.SnTracers.Select(snt => snt?.GetType().Name))}");
        }

        private void RegisterAppdomainEventHandlers()
        {
            AppDomain appDomain = AppDomain.CurrentDomain;
            appDomain.UnhandledException += new UnhandledExceptionEventHandler(Domain_UnhandledException);
            _shutdownActions.Push(("Unregistering AppDomain event handlers.",
                () => appDomain.UnhandledException -= Domain_UnhandledException));
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
            lock (_startStopSync)
            {
                if (_instance == null)
                {
                    _started = false;

                    SnLog.WriteWarning("Repository shutdown has already completed.");
                    return;
                }

                var instance = _instance;
                var errors = new List<Exception>();
                void TryExecute(Action action)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception error)
                    {
                        _isReusable = false;
                        errors.Add(error);
                    }
                }
                void TryShutdown((string Name, Action Action) step)
                {
                    // Diagnostic failures must not prevent the cleanup action from running.
                    TryExecute(() =>
                    {
                        if (instance._logger != null)
                            instance._logger.LogInformation("{ShutdownAction}", step.Name);
                        else
                            SnTrace.Repository.Write(step.Name);
                    });
                    TryExecute(() => instance.ConsoleWriteLine(false, step.Name));
                    TryExecute(step.Action);
                }

                try
                {
                    if (_started)
                    {
                        TryShutdown(("Sending a goodbye message.", () =>
                        {
                            var channel = Providers.Instance.ClusterChannelProvider;
                            channel.ClusterMemberInfo.NeedToRecover = false;
                            new PingMessage().SendAsync(CancellationToken.None).GetAwaiter().GetResult();
                        }));
                    }

                    // Continue cleanup after individual failures and unwind partial startup.
                    while (instance._shutdownActions.Count > 0)
                        TryShutdown(instance._shutdownActions.Pop());

                    TryShutdown(("Resetting ContextHandler.", ContextHandler.Reset));
                    TryShutdown(("Updating repository status.", () =>
                    {
                        var status = Providers.Instance.RepositoryStatus;
                        if (status != null)
                            status.IsRunning = false;
                    }));
                    TryShutdown(("Writing shutdown summary.", () =>
                    {
                        var t = DateTime.UtcNow - instance._startupInfo.Starting;
                        var msg = $"Repository has stopped. Running time: {t.Days}.{t.Hours:d2}:{t.Minutes:d2}:{t.Seconds:d2}";

                        if (instance._logger != null)
                            instance._logger.LogInformation(msg);
                        else
                            SnTrace.Repository.Write(msg);

                        SnTrace.Flush();
                        instance.ConsoleWriteLine(false, msg);
                        instance.ConsoleWriteLine(false);
                    }));
                }
                finally
                {
                    instance._disposed = true;
                    _instance = null;
                    _started = false;
                }

                if (errors.Count == 1)
                    ExceptionDispatchInfo.Capture(errors[0]).Throw();
                if (errors.Count > 1)
                    throw new AggregateException("Repository shutdown failed.", errors);
            }
        }

        public void ConsoleWrite(bool trace, params string[] text)
        {
            foreach (var s in text)
            {
                if(trace)
                    SnTrace.System.Write(s);
                _settings.Console?.Write(s);
            }
        }
        public void ConsoleWriteLine(bool trace, params string[] text)
        {
            ConsoleWrite(trace, text);
            _settings.Console?.WriteLine();
        }

        internal static bool Started()
        {
            return _started;
        }

        // ======================================== IndexingEngine hooks

        public static bool IndexingEngineIsRunning
        {
            get
            {
                if (_instance == null)
                    throw new NotSupportedException("Querying running state of IndexingEngine is not supported when RepositoryInstance is not created.");
                return Providers.Instance.IndexManager.Running;
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

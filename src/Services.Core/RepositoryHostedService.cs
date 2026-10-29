using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Storage.Diagnostics;
using SenseNet.Services.Core.Diagnostics;
using Exception = System.Exception;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core
{
    /// <summary>
    /// Background service for starting and stopping the sensenet Content Repository
    /// instance in a hosted environment.
    /// </summary>
    public class RepositoryHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<RepositoryHostedService> _logger;
        private readonly object _repositorySync = new();
        private readonly HealthRepositoryLifecycle _healthLifecycle;
        private readonly HealthChangeSignal _healthSignal;
        private bool _stopping;

        private IServiceProvider Services { get; }
        private RepositoryInstance Repository { get; set; }
        private Action<RepositoryBuilder, IServiceProvider> BuildRepository { get; }
        private Func<RepositoryInstance, IServiceProvider, Task> OnRepositoryStartedAsync { get; }

        public ISenseNetStatus RepositoryStatus { get; }

        public RepositoryHostedService(IServiceProvider provider, 
            Action<RepositoryBuilder, IServiceProvider> buildRepository,
            Func<RepositoryInstance, IServiceProvider, Task> onRepositoryStartedAsync)
        {
            Services = provider;
            BuildRepository = buildRepository;
            OnRepositoryStartedAsync = onRepositoryStartedAsync;
            RepositoryStatus = provider.GetService<ISenseNetStatus>();
            _logger = provider.GetService<ILogger<RepositoryHostedService>>();
            _healthLifecycle = provider.GetRequiredService<HealthRepositoryLifecycle>();
            _healthSignal = provider.GetRequiredService<HealthChangeSignal>();
        }

        public RepositoryBuilder BuildProviders()
        {
            // set provider instances for legacy code (task manager, preview provider)
            Services.AddSenseNetProviderInstances();

            var components = Services.GetServices<ISnComponent>().ToArray();
            var eventProcessors = Services.GetServices<IEventProcessor>().ToArray();

            var repositoryBuilder = new RepositoryBuilder(Services)
                .UseLogger(new SnFileSystemEventLogger())
                .UseComponent(components)
                .UseAccessProvider(new UserAccessProvider())
                .UseEventDistributor(new EventDistributor())
                .AddAsyncEventProcessors(eventProcessors)
                .UseTraceCategories("Event", "Custom", "System", "Security", "ContentOperation", "Index",
                    "Repository") as RepositoryBuilder;

            // hook for developers to modify the repository builder before start
            BuildRepository?.Invoke(repositoryBuilder, Services);

            return repositoryBuilder;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RepositoryStatus.SetStatus(SenseNetStatus.Starting);

            _startupTask = Task.Run(async () =>
            {
                try
                {
                    var repositoryBuilder = BuildProviders();
                    var repository = ContentRepository.Repository.Start(repositoryBuilder);
                    var keepRepository = false;
                    lock (_repositorySync)
                    {
                        if (!_stopping && !_healthLifecycle.FailedOrStopping)
                        {
                            Repository = repository;
                            keepRepository = true;
                        }
                    }
                    if (!keepRepository)
                    {
                        repository.Dispose();
                        RepositoryStatus.IsRunning = false;
                        return;
                    }
                    if (OnRepositoryStartedAsync != null)
                        await OnRepositoryStartedAsync.Invoke(repository, Services).ConfigureAwait(false);

                    lock (_repositorySync)
                    {
                        if (_stopping || _healthLifecycle.FailedOrStopping)
                        {
                            RepositoryStatus.IsRunning = false;
                            return;
                        }
                        _healthLifecycle.SetReady();
                        RepositoryStatus.IsRunning = true;
                        RepositoryStatus.SetStatus(SenseNetStatus.Started);
                    }
                    _logger.LogDebug("STARTUP: Repository started");
                }
                catch (Exception ex)
                {
                    _healthLifecycle.Stop();
                    RepositoryStatus.IsRunning = false;
                    lock (_repositorySync)
                        RepositoryStatus.SetStatus(_stopping ? SenseNetStatus.Stopped : "Startup failed");
                    _logger.LogError(ex, "Error during repository start.");
                }
                _healthSignal.Notify();
            }, cancellationToken);

            return Task.CompletedTask;
        }

        private Task _startupTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            RepositoryInstance repository;
            lock (_repositorySync)
            {
                _stopping = true;
                _healthLifecycle.Stop();
                repository = Repository;
                Repository = null;
            }
            _healthSignal.Notify();
            if (repository != null)
            {
                RepositoryStatus.IsRunning = false;
                RepositoryStatus.SetStatus(SenseNetStatus.Stopping);

                // shut down the repository
                repository.Dispose();
            }

            RepositoryStatus.SetStatus(SenseNetStatus.Stopped);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                RepositoryInstance repository;
                lock (_repositorySync)
                {
                    _stopping = true;
                    _healthLifecycle.Stop();
                    repository = Repository;
                    Repository = null;
                }
                repository?.Dispose();
            }

            _disposed = true;
        }
    }
}

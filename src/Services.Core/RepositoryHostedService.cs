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

            var repositoryBuilder = BuildProviders();

            try
            {
                _startupTask = Task.Run(
                    async () =>
                    {
                        Repository = ContentRepository.Repository.Start(repositoryBuilder);

                        if (OnRepositoryStartedAsync != null)
                            await OnRepositoryStartedAsync.Invoke(Repository, Services);

                        RepositoryStatus.IsRunning = true;
                        RepositoryStatus.SetStatus(SenseNetStatus.Started);
                        _logger.LogDebug("STARTUP: Repository started");
                    }, cancellationToken);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, $"Error during repository start: {ex.Message}");
                throw;
            }

            return Task.CompletedTask;
        }

        private Task _startupTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (Repository != null)
            {
                RepositoryStatus.IsRunning = false;
                RepositoryStatus.SetStatus(SenseNetStatus.Stopping);

                // shut down the repository
                Repository?.Dispose();

                Repository = null;
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
                Repository?.Dispose();
            }

            _disposed = true;
        }
    }
}

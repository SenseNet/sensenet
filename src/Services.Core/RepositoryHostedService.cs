using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Events;
using SenseNet.Extensions.DependencyInjection;
using Exception = System.Exception;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core
{
    /// <summary>
    /// Background service for starting and stopping the sensenet Content Repository
    /// instance in a hosted environment.
    /// </summary>
    internal class RepositoryHostedService : IHostedService, IDisposable
    {
        private IServiceProvider Services { get; }
        private RepositoryInstance Repository { get; set; }
        private Action<RepositoryBuilder, IServiceProvider> BuildRepository { get; }
        private Func<RepositoryInstance, IServiceProvider, Task> OnRepositoryStartedAsync { get; }

        public RepositoryHostedService(IServiceProvider provider, 
            Action<RepositoryBuilder, IServiceProvider> buildRepository,
            Func<RepositoryInstance, IServiceProvider, Task> onRepositoryStartedAsync)
        {
            Services = provider;
            BuildRepository = buildRepository;
            OnRepositoryStartedAsync = onRepositoryStartedAsync;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // set provider instances for legacy code (task manager, preview provider)
            Services.AddSenseNetProviderInstances();

            var components = Services.GetServices<ISnComponent>().ToArray();
            var eventProcessors = Services.GetServices<IEventProcessor>().ToArray();

            var repositoryBuilder = new RepositoryBuilder(Services)
                .UseLogger(new SnFileSystemEventLogger())
                .UseTracer(new SnFileSystemTracer())
                .UseComponent(components)
                .UseAccessProvider(new UserAccessProvider())
                .UseDataProvider(new MsSqlDataProvider())
                .UsePackagingDataProviderExtension(new MsSqlPackagingDataProvider())
                .StartWorkflowEngine(false)
                .UseEventDistributor(new EventDistributor())
                .AddAsyncEventProcessors(eventProcessors)
                .UseTraceCategories("Event", "Custom", "System", "Security") as RepositoryBuilder;

            // hook for developers to modify the repository builder before start
            BuildRepository?.Invoke(repositoryBuilder, Services);

            try
            {
                Repository = ContentRepository.Repository.Start(repositoryBuilder);

                if (OnRepositoryStartedAsync != null)
                    await OnRepositoryStartedAsync.Invoke(Repository, Services);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, $"Error during repository start: {ex.Message}");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // shut down the repository
            Repository?.Dispose();

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

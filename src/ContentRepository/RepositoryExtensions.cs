using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Packaging;
using SenseNet.ContentRepository.Storage;
using SenseNet.Preview;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Adds the default document provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetDocumentPreviewProvider(this IServiceCollection services)
        {
            // add the default, empty implementation
            return services.AddSenseNetDocumentPreviewProvider<DefaultDocumentPreviewProvider>();
        }
        /// <summary>
        /// Adds the provided document provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetDocumentPreviewProvider<T>(this IServiceCollection services) where T : DocumentPreviewProvider
        {
            return services.AddSingleton<IPreviewProvider, T>();
        }

        /// <summary>
        /// Adds the default ILatestComponentStore implementation to the service collection.
        /// </summary>
        public static IServiceCollection AddLatestComponentStore(this IServiceCollection services)
        {
            // add the default, empty implementation
            return services.AddLatestComponentStore<DefaultLatestComponentStore>();
        }
        /// <summary>
        /// Adds the provided ILatestComponentStore implementation to the service collection.
        /// </summary>
        public static IServiceCollection AddLatestComponentStore<T>(this IServiceCollection services)
            where T : class, ILatestComponentStore
        {
            return services.AddSingleton<ILatestComponentStore, T>();
        }

        /// <summary>
        /// Adds an <see cref="ISnComponent"/> to the service collection so that the system can
        /// collect components and their patches during repository start.
        /// </summary>
        public static IServiceCollection AddComponent(this IServiceCollection services, 
            Func<IServiceProvider, ISnComponent> createComponent)
        {
            // register this as transient so that no singleton instances remain in memory after creating them once
            services.AddTransient(createComponent);

            return services;
        }

        public static IServiceCollection AddRepositoryComponents(this IServiceCollection services)
        {
            services.AddComponent(provider => new ServicesComponent());

            return services;
        }
    }
}

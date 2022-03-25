using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Email;
using SenseNet.ContentRepository.Packaging;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging;
using SenseNet.Preview;
using SenseNet.Search;
using SenseNet.Search.Indexing;
using SenseNet.Search.Querying;

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
        
        /// <summary>
        /// Adds the installer information of the core sensenet package to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetInstallPackage(this IServiceCollection services, 
            Assembly assembly, string installPackageName)
        {
            services.AddSingleton<IInstallPackageDescriptor>(provider => new InstallPackageDescriptor(assembly, installPackageName));

            return services;
        }

        /// <summary>
        /// Adds the provided search engine to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetSearchEngine<T>(this IServiceCollection services) where T : class, ISearchEngine
        {
            return services.AddSingleton<ISearchEngine, T>();
        }
        /// <summary>
        /// Adds the provided search engine to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetSearchEngine(this IServiceCollection services, ISearchEngine searchEngine)
        {
            return services.AddSingleton(providers => searchEngine);
        }

        /// <summary>
        /// Adds the provided indexing engine to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetIndexingEngine<T>(this IServiceCollection services) where T : class, IIndexingEngine
        {
            return services.AddSingleton<IIndexingEngine, T>();
        }
        /// <summary>
        /// Adds the provided query engine to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetQueryEngine<T>(this IServiceCollection services) where T : class, IQueryEngine
        {
            return services.AddSingleton<IQueryEngine, T>();
        }

        /// <summary>
        /// Adds the provided api key manager to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetApiKeyManager<T>(this IServiceCollection services) where T : class, IApiKeyManager
        {
            return services.AddSingleton<IApiKeyManager, T>();
        }
        /// <summary>
        /// Adds the default api key manager to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetApiKeys(this IServiceCollection services)
        {
            return services.AddSenseNetApiKeyManager<ApiKeyManager>();
        }

        public static IServiceCollection AddSenseNetEmailManager(this IServiceCollection services, 
            Action<EmailOptions> configureSmtp = null)
        {
            return services
                .AddSingleton<IEmailTemplateManager, RepositoryEmailTemplateManager>()
                .AddSingleton<IEmailSender, EmailSender>()
                .Configure<EmailOptions>(options => { configureSmtp?.Invoke(options);});
        }


        /// <summary>
        /// Adds the provided template replacer to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetTemplateReplacer<T>(this IServiceCollection services) where T : TemplateReplacerBase
        {
            return services.AddSingleton<TemplateReplacerBase, T>();
        }
    }
}

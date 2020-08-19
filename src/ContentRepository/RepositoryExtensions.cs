using Microsoft.Extensions.DependencyInjection;
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
    }
}

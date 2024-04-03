using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class BlobStorageExtensions
    {
        /// <summary>
        /// Adds the blob metadata provider to the service collection.
        /// </summary>
        public static IServiceCollection AddSenseNetBlobStorageMetaDataProvider<T>(this IServiceCollection services) 
            where T : class, IBlobStorageMetaDataProvider
        {
            services.AddSingleton<IBlobStorageMetaDataProvider, T>();

            return services;
        }
    }
}

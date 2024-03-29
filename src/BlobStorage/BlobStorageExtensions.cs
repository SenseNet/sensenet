﻿using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class BlobStorageExtensions
    {
        /// <summary>
        /// Set the external blob provider to be used by the built-in blob provider selector
        /// during write operations when the binary size exceeds a configured value.
        /// </summary>
        [Obsolete("Do not use this method anymore. Register blob providers as services instead.", true)]
        public static IRepositoryBuilder UseExternalBlobProvider(this IRepositoryBuilder builder, IBlobProvider provider)
        {
            return builder;
        }

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

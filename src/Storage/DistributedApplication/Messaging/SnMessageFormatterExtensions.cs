using Microsoft.Extensions.DependencyInjection;
using SenseNet.Communication.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Storage.DistributedApplication.Messaging;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class SnMessageFormatterBaseExtensions
    {
        public static IServiceCollection AddClusterMessageType<T>(this IServiceCollection services) where T : ClusterMessage
        {
            return services.AddSingleton<ClusterMessageType>(new ClusterMessageType(typeof(T)));
        }
    }
}

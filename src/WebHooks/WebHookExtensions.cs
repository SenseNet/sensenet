﻿using Microsoft.Extensions.DependencyInjection;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Events;
using SenseNet.WebHooks;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class WebHookExtensions
    {
        /// <summary>
        /// Adds the WebHooks component and the required related services.
        /// </summary>
        public static IServiceCollection AddSenseNetWebHooks(this IServiceCollection services)
        {
            services.AddComponent(provider => new WebHookComponent());

            services.AddHttpClient();
            services.AddSenseNetWebHookClient<HttpWebHookClient>();
            services.AddSenseNetTemplateReplacer<WebHooksTemplateReplacer>();

            services.AddSingleton<IEventProcessor, LocalWebHookProcessor>();
            services.AddSingleton<IWebHookSubscriptionStore, BuiltInWebHookSubscriptionStore>();

            // add the same processor instance as an IWebHookEventProcessor service (forwarding)
            services.AddSingleton(providers => 
                providers.GetServices<IEventProcessor>().FirstOrDefault(p => p is IWebHookEventProcessor) as IWebHookEventProcessor);

            services.AddStatisticalDataAggregator<WebHookStatisticalDataAggregator>();

            return services;
        }

        /// <summary>
        /// Finds and parses the version number of the previous node version
        /// in the changed data stored in the node event args.
        /// </summary>
        /// <returns>The parsed version number or null.</returns>
        internal static VersionNumber GetPreviousVersion(this NodeEventArgs eventArgs)
        {
            var chv = eventArgs?.ChangedData?.FirstOrDefault(cd => cd.Name == "Version");
            if (chv == null)
                return null;

            return VersionNumber.TryParse((string)chv.Original, out var oldVersion) ? oldVersion : null;
        }
    }
}

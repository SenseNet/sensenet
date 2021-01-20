using Microsoft.Extensions.DependencyInjection;
using SenseNet.WebHooks.Common;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class WebHookExtensions
    {
        public static IServiceCollection AddSenseNetWebHookClient<T>(this IServiceCollection services) where T : class, IWebHookClient
        {
            //UNDONE: maybe register HttpClient only if T is the default class
            services.AddHttpClient();
            services.AddSingleton<IWebHookClient, T>();

            return services;
        }
    }
}

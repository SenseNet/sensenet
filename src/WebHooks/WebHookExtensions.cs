using Microsoft.Extensions.DependencyInjection;
using SenseNet.WebHooks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class WebHookExtensions
    {
        public static IServiceCollection AddSenseNetWebHookProcessor(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSenseNetWebHookClient<HttpWebHookClient>();

            //UNDONE: use the official Event processor registration method when ready
            services.AddSingleton<IEventProcessor, LocalWebHookProcessor>();

            //UNDONE: use the real IWebHookFilter implementation when ready
            services.AddSingleton<IWebHookFilter, NullWebHookFilter>();

            return services;
        }
    }
}

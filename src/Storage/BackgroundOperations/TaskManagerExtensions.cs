using System;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.BackgroundOperations;
using SenseNet.TaskManagement.Core;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class TaskManagerExtensions
    {
        public static IServiceCollection AddSenseNetTaskManager(this IServiceCollection services)
        {
            return services.AddSenseNetTaskManager<TaskManagerBase>();
        }
        public static IServiceCollection AddSenseNetTaskManager<T>(this IServiceCollection services) where T : class, ITaskManager
        {
            services.AddSingleton<ITaskManager, T>();
            services.AddTaskManagementClient();

            return services;
        }

        [Obsolete("Use TaskManagementOptions from the services collection instead.")]
        public static string GetUrlOrSetting(this TaskManagement.Core.TaskManagementOptions options)
        {
            return !string.IsNullOrEmpty(options?.Url) ? options.Url : SnTaskManager.Settings.TaskManagementUrl;
        }
        [Obsolete("Use TaskManagementOptions from the services collection instead.")]
        public static string GetApplicationUrlOrSetting(this TaskManagement.Core.TaskManagementOptions options)
        {
            return !string.IsNullOrEmpty(options?.ApplicationUrl) ? options.ApplicationUrl : SnTaskManager.Settings.AppUrl;
        }
        [Obsolete("Use TaskManagementOptions from the services collection instead.")]
        public static string GetApplicationIdOrSetting(this TaskManagement.Core.TaskManagementOptions options)
        {
            return !string.IsNullOrEmpty(options?.ApplicationId) ? options.ApplicationId : SnTaskManager.Settings.AppId;
        }
    }
}

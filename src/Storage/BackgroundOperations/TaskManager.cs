using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    public static class SnTaskManager
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static class Settings
        {
            public static readonly string SETTINGSNAME = "TaskManagement";
            public static readonly string TASKMANAGEMENTURL = "TaskManagementUrl";
            public static readonly string TASKMANAGEMENTAPPLICATIONURL = "TaskManagementApplicationUrl";
            public static readonly string TASKMANAGEMENTAPPID = "TaskManagementAppId";

            public static readonly string TASKMANAGEMENTDEFAULTAPPID = "SenseNet1";

            [Obsolete("Use TaskManagementOptions from the services collection instead.")]
            public static string AppId =>
                ContentRepository.Storage.Settings.GetValue(SETTINGSNAME, TASKMANAGEMENTAPPID, null, "SenseNet");
            [Obsolete("Use TaskManagementOptions from the services collection instead.")]
            public static string AppUrl =>
                ContentRepository.Storage.Settings.GetValue<string>(SETTINGSNAME, TASKMANAGEMENTAPPLICATIONURL);

            /// <summary>
            /// Url of the Task management web application, directly from the TaskManagement setting.
            /// New applications should rely on the TaskManagementOptions configuration object
            /// available from the services collection.
            /// </summary>
            [Obsolete("Use TaskManagementOptions from the services collection instead.")]
            public static string TaskManagementUrl =>
                ContentRepository.Storage.Settings.GetValue<string>(SETTINGSNAME, TASKMANAGEMENTURL);
        }

        /// <summary>
        /// Url of the Task management web application, directly from the TaskManagement setting.
        /// New applications should rely on the TaskManagementOptions configuration object
        /// available from the services collection.
        /// </summary>
        [Obsolete("Use TaskManagementOptions from the services collection instead.")]
        public static string Url => Settings.TaskManagementUrl;

        // ================================================================================== Static API

        /// <summary>
        /// Registers a task through the task management API. 
        /// </summary>
        /// <returns>Returns a RegisterTaskResult object containing information about the registered task.</returns>
        [Obsolete("Use the ITaskManager service registered in the dependency injection container instead.")]
        public static RegisterTaskResult RegisterTask(RegisterTaskRequest requestData)
        {
            return Instance.RegisterTaskAsync(requestData, CancellationToken.None).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Registers a task through the task management API asynchronously.
        /// </summary>
        /// <returns>Returns a RegisterTaskResult object containing information about the registered task.</returns>
        [Obsolete("Use the ITaskManager service registered in the dependency injection container instead.")]
        public static Task<RegisterTaskResult> RegisterTaskAsync(RegisterTaskRequest requestData)
        {
            return Instance.RegisterTaskAsync(requestData, CancellationToken.None);
        }

        /// <summary>
        /// Registers an application through the task management API.
        /// </summary>
        /// <returns>Returns true if the registration was successful.</returns>
        [Obsolete("Use the ITaskManager service registered in the dependency injection container instead.")]
        public static bool RegisterApplication()
        {
            return Instance.RegisterApplicationAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Built-in helper method for logging task execution results. Call this from every custom task finalizer.
        /// </summary>
        /// <param name="result"></param>
        [Obsolete("Use the ITaskManager service registered in the dependency injection container instead.")]
        public static void OnTaskFinished(SnTaskResult result)
        {
            Instance.OnTaskFinishedAsync(result, CancellationToken.None).GetAwaiter().GetResult();
        }

        // ================================================================================== Instance


        /// <summary>
        /// Gets the Task manager instance, a singleton used by legacy code.
        /// </summary>
        [Obsolete("Use the service through the dependency injection framework instead.")]
        public static ITaskManager Instance => Providers.Instance.TaskManager;
    }

    public class TaskManagerBase : ITaskManager
    {
        private readonly ITaskManagementClient _client;
        private readonly TaskManagementOptions _options;
        
        public TaskManagerBase(ITaskManagementClient client, IOptions<TaskManagementOptions> options)
        {
            _client = client;
            _options = options?.Value ?? new TaskManagementOptions();
        }

        public virtual async Task<RegisterTaskResult> RegisterTaskAsync(RegisterTaskRequest requestData, CancellationToken cancellationToken)
        {
            var taskManagementUrl = _options.GetUrlOrSetting();
            if (string.IsNullOrEmpty(taskManagementUrl) || requestData == null)
                return null;

            while (true)
            {
                try
                {
                    return await _client.RegisterTaskAsync(requestData).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // look for a special error message about an unknown app id
                    if (ex is TaskManagementException && string.CompareOrdinal(ex.Message, RegisterTaskRequest.ERROR_UNKNOWN_APPID) == 0)
                    {
                        // try to re-register the app
                        if (await RegisterApplicationAsync(cancellationToken))
                        {
                            // skip error logging and try to register the task again
                            continue;
                        }
                    }

                    SnLog.WriteException(ex, "Error during task registration.",
                        EventId.TaskManagement.General,
                        properties: new Dictionary<string, object>
                        {
                            {"TaskManagementUrl", taskManagementUrl},
                            {"Type", requestData.Type},
                            {"Title", requestData.Title},
                            {"Data", requestData.TaskData}
                        });

                    // do not retry again after a real error
                    break;
                }
            }

            // no need to throw an exception, we already logged the error
            return null;
        }

        public virtual async Task<bool> RegisterApplicationAsync(CancellationToken cancellationToken)
        {
            var taskManagementUrl = _options.GetUrlOrSetting();
            if (string.IsNullOrEmpty(taskManagementUrl))
            {
                SnTrace.TaskManagement.Write("Task management url is empty, application is not registered.");
                return false;
            }

            var requestData = new RegisterApplicationRequest
            {
                AppId = _options.GetApplicationIdOrSetting(),
                ApplicationUrl = _options.GetApplicationUrlOrSetting()
            };

            try
            {
                await _client.RegisterApplicationAsync(requestData).ConfigureAwait(false);

                SnLog.WriteInformation("Task management app registration was successful.", EventId.TaskManagement.General, properties: new Dictionary<string, object>
                {
                    { "TaskManagementUrl", taskManagementUrl },
                    { "AppId", requestData.AppId }
                });

                return true;
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, "Error during app registration.", EventId.TaskManagement.General,
                    properties: new Dictionary<string, object>
                    {
                        {"TaskManagementUrl", taskManagementUrl},
                        {"AppId", requestData.AppId},
                        {"ApplicationUrl", requestData.ApplicationUrl}
                    });
            }

            // no need to throw an exception, we already logged the error
            return false;
        }

        public virtual Task OnTaskFinishedAsync(SnTaskResult result, CancellationToken cancellationToken)
        {
            // the task was executed successfully without an error message
            if (result.Successful && result.Error == null)
                return Task.CompletedTask;

            try
            {
                if (result.Error != null)
                {
                    // log the error message and details for admins
                    SnLog.WriteError("Task execution error, see the details below.",
                        EventId.TaskManagement.General,
                        properties: new Dictionary<string, object>
                        {
                            {"TaskType", result.Task.Type},
                            {"TaskData", result.Task.TaskData},
                            {"ErrorCode", result.Error.ErrorCode},
                            {"ErrorType", result.Error.ErrorType},
                            {"Message", result.Error.Message},
                            {"Details", result.Error.Details},
                            {"CallingContext", result.Error.CallingContext}
                        });
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return Task.CompletedTask;
        }
    }
}

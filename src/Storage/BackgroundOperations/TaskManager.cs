using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.TaskManagement.Core;
using SenseNet.Tools;

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
        }

        /// <summary>
        /// Url of the Task management web application, directly from the TaskManagement setting.
        /// </summary>
        public static string Url =>
            ContentRepository.Storage.Settings.GetValue<string>(Settings.SETTINGSNAME, Settings.TASKMANAGEMENTURL);

        // ================================================================================== Static API

        /// <summary>
        /// Registers a task through the task management API.
        /// If possible, use the asynchronous version of this method.
        /// </summary>
        /// <returns>Returns a RegisterTaskResult object containing information about the registered task.</returns>
        public static RegisterTaskResult RegisterTask(RegisterTaskRequest requestData)
        {
            // start the task registration process only if a url is provided
            var taskManUrl = Url;
            if (string.IsNullOrEmpty(taskManUrl))
                return null;

            // make this a synchron call
            return Instance.RegisterTaskAsync(taskManUrl, requestData).Result;
        }
        /// <summary>
        /// Registers a task through the task management API asynchronously.
        /// </summary>
        /// <returns>Returns a RegisterTaskResult object containing information about the registered task.</returns>
        public static Task<RegisterTaskResult> RegisterTaskAsync(RegisterTaskRequest requestData)
        {
            // start the task registration process only if a url is provided
            var taskManUrl = Url;

            return string.IsNullOrEmpty(taskManUrl)
                ? Task.FromResult<RegisterTaskResult>(null)
                : Instance.RegisterTaskAsync(taskManUrl, requestData);
        }

        /// <summary>
        /// Registers an application through the task management API.
        /// </summary>
        /// <returns>Returns true if the registration was successful.</returns>
        public static bool RegisterApplication()
        {
            var taskManUrl = Url;
            if (string.IsNullOrEmpty(taskManUrl))
                return false;

            var requestData = new RegisterApplicationRequest
            {
                AppId = ContentRepository.Storage.Settings.GetValue(Settings.SETTINGSNAME, Settings.TASKMANAGEMENTAPPID, null, Settings.TASKMANAGEMENTDEFAULTAPPID),
                ApplicationUrl = ContentRepository.Storage.Settings.GetValue<string>(Settings.SETTINGSNAME, Settings.TASKMANAGEMENTAPPLICATIONURL)
            };

            // make this a synchron call
            var registered = Instance.RegisterApplicationAsync(taskManUrl, requestData).Result;

            if (registered)
            {
                SnLog.WriteInformation("Task management app registration was successful.", EventId.TaskManagement.General, properties: new Dictionary<string, object>
                {
                    { "TaskManagementUrl", taskManUrl },
                    { "AppId", requestData.AppId }
                });
            }

            return registered;
        }

        /// <summary>
        /// Built-in helper method for logging task execution results. Call this from every custom task finalizer.
        /// </summary>
        /// <param name="result"></param>
        public static void OnTaskFinished(SnTaskResult result)
        {
            Instance.OnTaskFinished(result);
        }

        // ================================================================================== Instance

        private static readonly object InitializationLock = new object();
        private static ITaskManager _instance;
        private static ITaskManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InitializationLock)
                    {
                        if (_instance == null)
                        {
                            ITaskManager instance;

                            if (string.IsNullOrEmpty(Providers.TaskManagerClassName))
                            {
                                instance = new TaskManagerBase();
                            }
                            else
                            {
                                try
                                {
                                    instance = (ITaskManager)TypeResolver.CreateInstance(Providers.TaskManagerClassName);
                                }
                                catch (Exception)
                                {
                                    SnLog.WriteWarning("Error loading task manager type " + Providers.TaskManagerClassName,
                                        EventId.RepositoryLifecycle);

                                    instance = new TaskManagerBase();
                                }
                            }

                            SnLog.WriteInformation("TaskManager created: " + instance);

                            _instance = instance;
                        }
                    }
                }
                return _instance;
            }
        }
    }

    public class TaskManagerBase : ITaskManager
    {
        public virtual async Task<RegisterTaskResult> RegisterTaskAsync(string taskManagementUrl, RegisterTaskRequest requestData)
        {
            while (true)
            {
                try
                {
                    return await/*undone*/ RepositoryClient.RegisterTaskAsync(taskManagementUrl, requestData).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // look for a special error message about an unknown app id
                    if (ex is TaskManagementException && string.CompareOrdinal(ex.Message, RegisterTaskRequest.ERROR_UNKNOWN_APPID) == 0)
                    {
                        // try to re-register the app
                        if (SnTaskManager.RegisterApplication())
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

        public virtual async Task<bool> RegisterApplicationAsync(string taskManagementUrl, RegisterApplicationRequest requestData)
        {
            try
            {
                await/*undone*/ RepositoryClient.RegisterApplicationAsync(taskManagementUrl, requestData).ConfigureAwait(false);

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

        public virtual void OnTaskFinished(SnTaskResult result)
        {
            // the task was executed successfully without an error message
            if (result.Successful && result.Error == null)
                return;

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
        }
    }
}

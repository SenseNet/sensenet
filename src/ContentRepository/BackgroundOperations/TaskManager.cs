using System;
using SenseNet.TaskManagement.Core;

namespace SenseNet.BackgroundOperations
{
    [Obsolete("Use SnTaskManager found in the Storage dll.")]
    public static class TaskManager
    {
        [Obsolete("Use SnTaskManager found in the Storage dll.")]
        public static class Settings
        {
            public static readonly string SETTINGSNAME = "TaskManagement";
            public static readonly string TASKMANAGEMENTURL = "TaskManagementUrl";
            public static readonly string TASKMANAGEMENTAPPLICATIONURL = "TaskManagementApplicationUrl";
            public static readonly string TASKMANAGEMENTAPPID = "TaskManagementAppId";

            public static readonly string TASKMANAGEMENTDEFAULTAPPID = "SenseNet1";
        }

        [Obsolete("Use SnTaskManager found in the Storage dll.")]
        public static string TaskManagementUrl
        {
            get { return SnTaskManager.Url; }
        }

        // ================================================================================== Static API

        [Obsolete("Use SnTaskManager found in the Storage dll.")]
        public static RegisterTaskResult RegisterTask(RegisterTaskRequest requestData)
        {
            return SnTaskManager.RegisterTask(requestData);
        }

        [Obsolete("Use SnTaskManager found in the Storage dll.")]
        public static bool RegisterApplication()
        {
            return SnTaskManager.RegisterApplication();
        }

        [Obsolete("Use SnTaskManager found in the Storage dll.")]
        public static void OnTaskFinished(SnTaskResult result)
        {
            SnTaskManager.OnTaskFinished(result);
        }
    }

    [Obsolete("Inherit from SenseNet.BackgroundOperations.TaskManagerBase, found in the Storage dll.")]
    public class DefaultTaskManager : TaskManagerBase
    {

    }
}

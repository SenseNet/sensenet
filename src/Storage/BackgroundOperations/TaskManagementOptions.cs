// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    public class TaskManagementOptions
    {
        public string Url { get; set; }
        internal string UrlOrSetting => !string.IsNullOrEmpty(Url) ? Url : SnTaskManager.Settings.TaskManagementUrl;

        public string ApplicationUrl { get; set; }
        internal string ApplicationUrlOrSetting =>
            !string.IsNullOrEmpty(ApplicationUrl) ? ApplicationUrl : SnTaskManager.Settings.AppUrl;

        public string ApplicationId { get; set; }
        internal string ApplicationIdOrSetting =>
            !string.IsNullOrEmpty(ApplicationId) ? ApplicationId : SnTaskManager.Settings.AppId;
    }
}

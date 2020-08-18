// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    public class TaskManagementOptions
    {
        public string Url { get; set; }
        /// <summary>
        /// Internal fallback property.
        /// </summary>
#pragma warning disable 618
        internal string UrlOrSetting => !string.IsNullOrEmpty(Url) ? Url : SnTaskManager.Settings.TaskManagementUrl;

        public string ApplicationUrl { get; set; }
        /// <summary>
        /// Internal fallback property.
        /// </summary>
        internal string ApplicationUrlOrSetting =>
            !string.IsNullOrEmpty(ApplicationUrl) ? ApplicationUrl : SnTaskManager.Settings.AppUrl;

        public string ApplicationId { get; set; }
        /// <summary>
        /// Internal fallback property.
        /// </summary>
        internal string ApplicationIdOrSetting =>
            !string.IsNullOrEmpty(ApplicationId) ? ApplicationId : SnTaskManager.Settings.AppId;
#pragma warning restore 618
    }
}

// ReSharper disable once CheckNamespace
namespace SenseNet.BackgroundOperations
{
    public class TaskManagementOptions
    {
        private string _url;
        private string _applicationUrl;
        private string _applicationId;

        public string Url
        {
            get => !string.IsNullOrEmpty(_url) ? _url : SnTaskManager.Settings.TaskManagementUrl;
            set => _url = value;
        }

        public string ApplicationUrl
        {
            get => !string.IsNullOrEmpty(_applicationUrl) ? _applicationUrl : SnTaskManager.Settings.AppUrl;
            set => _applicationUrl = value;
        }

        public string ApplicationId
        {
            get => !string.IsNullOrEmpty(_applicationId) ? _applicationId : SnTaskManager.Settings.AppId;
            set => _applicationId = value;
        }
    }
}

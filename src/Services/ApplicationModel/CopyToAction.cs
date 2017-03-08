using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.OData;

namespace SenseNet.ApplicationModel
{
    public class CopyToAction : OpenPickerAction
    {
        protected override string GetCallBackScript()
        {
            return GetServiceCallBackScript(
                url: ODataTools.GetODataOperationUrl(Content, "CopyTo", true),
                scriptBeforeServiceCall: "var path = '" + Content.Path + "'",
                postData: "JSON.stringify({ targetPath: targetPath })",
                inprogressTitle: SenseNetResourceManager.Current.GetString("Action", "CopyInProgressDialogTitle"),
                successContent: SenseNetResourceManager.Current.GetString("Action", "CopyDialogContent"),
                successTitle: SenseNetResourceManager.Current.GetString("Action", "CopyDialogTitle"),
                successCallback: @"SN.Util.RefreshExploreTree([targetPath]);",
                errorCallback: @"SN.Util.RefreshExploreTree([targetPath]);",
                successCallbackAfterDialog: "location = location;",
                errorCallbackAfterDialog: "location = location;"
                );
        }

        public override bool IsODataOperation { get { return true; } }
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("targetPath", typeof(string), true) };

        public override object Execute(Content content, params object[] parameters)
        {
            Node.Copy(content.Path, (string)parameters[0]);
            return null;
        }
    }
}

using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.OData;

namespace SenseNet.ApplicationModel
{
    public class MoveToAction : OpenPickerAction
    {
        protected override string GetCallBackScript()
        {
            var callbackScript = GetServiceCallBackScript(
                url: ODataTools.GetODataOperationUrl(Content, "MoveTo", true),
                scriptBeforeServiceCall: "var path = '" + Content.Path + "'",
                postData: "JSON.stringify({ targetPath: targetPath })",
                inprogressTitle: SenseNetResourceManager.Current.GetString("Action", "MoveInProgressDialogTitle"),
                successContent: SenseNetResourceManager.Current.GetString("Action", "MoveDialogContent"),
                successTitle: SenseNetResourceManager.Current.GetString("Action", "MoveDialogTitle"),
                successCallback: @"
var pathToRefresh = SN.Util.GetParentPath(path);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                errorCallback: @"
var pathToRefresh = SN.Util.GetParentPath(path);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                successCallbackAfterDialog: @"
location=" + (this.RedirectToBackUrl ? string.Concat("\'", HttpUtility.UrlDecode(this.BackUri), "\'") : "SN.Util.GetParentUrlForPath(path)") + ";",
                errorCallbackAfterDialog: "location=location;"
                );
            
            return callbackScript;
        }


        public override bool IsODataOperation { get { return true; } }
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("targetPath", typeof(string), true) };

        public override object Execute(Content content, params object[] parameters)
        {
            Node.Move(content.Path, (string)parameters[0]);
            return null;
        }
    }
}

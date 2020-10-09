using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Services;

namespace SenseNet.ApplicationModel
{
    public class OpenPickerAction : ClientAction
    {
        protected virtual string DefaultPath
        {
            get
            {
                if (this.Content == null)
                    return null;

                var parentPath = this.Content.ContentHandler.ParentPath;
                return string.IsNullOrEmpty(parentPath) ? null : parentPath;
            }
        }

        protected virtual string MultiSelectMode { get; } = "none";

        protected virtual string TargetActionName
        {
            get { throw new SnNotSupportedException(); }
        }

        protected virtual string TargetParameterName { get; } = "sourceids";

        protected virtual string GetOpenContentPickerScript()
        {
            if (Content == null || this.Forbidden)
                return string.Empty;

            var rootPathString = UITools.GetGetContentPickerRootPathString(Content.Path);

            var script = this.DefaultPath != null 
                ? $"if ($(this).hasClass('sn-disabled')) return false; SN.PickerApplication.open({{ MultiSelectMode: '{MultiSelectMode}', callBack: {GetCallBackScript()}, TreeRoots: {rootPathString}, DefaultPath: '{DefaultPath}', target: this }});" 
                : $"if ($(this).hasClass('sn-disabled')) return false; SN.PickerApplication.open({{ MultiSelectMode: '{MultiSelectMode}', callBack: {GetCallBackScript()}, TreeRoots: {rootPathString}, target: this}});";

            return script;
        }

        protected virtual string GetCallBackScript()
        {
            return $@"function(resultData) {{if (!resultData) return; var targetPath = resultData[0].Path; var idlist = {GetIdList()}; var requestPath = targetPath + '?action={TargetActionName}&{TargetParameterName}=' + idlist + '&back=' + escape(window.location.href); window.location = requestPath;}}";
        }

        protected string GetServiceCallBackScript(string url, string scriptBeforeServiceCall, string postData, string inprogressTitle, string successContent, string successTitle, string successCallback, string errorCallback, string successCallbackAfterDialog, string errorCallbackAfterDialog)
        {
            var callback = string.Concat(
@"function(resultData) {
    if (!resultData) return; 
    var waitdlg = SN.Util.CreateWaitDialog('", inprogressTitle, @"');
    var targetPath = resultData[0].Path;", scriptBeforeServiceCall, @"; 
    $.ajax({
        url: '", url, @"', 
        type:'POST',
        cache:false, 
        data: ", postData, @", 
        success: function(data) {
            waitdlg.close(); 
            ", successCallback, @"
            SN.Util.CreateStatusDialog('", successContent, @"', '", successTitle, @"', function() {", successCallbackAfterDialog, @"}); 
        },
        error: function(response) { 
            waitdlg.close(); 
            ", errorCallback, @"
            var respObj = JSON.parse(response.responseText); 
            SN.Util.CreateErrorDialog(respObj.error.message.value, '", SenseNetResourceManager.Current.GetString("Action", "ErrorDialogTitle"), @"', function() {", errorCallbackAfterDialog, @"});
        }
    }); 
}");

            return callback;
        }

        protected virtual string GetIdList()
        {
            return Content?.Id.ToString() ?? string.Empty;
        }

        protected string GetPathListMethod()
        {
            var parameters = GetParameters();
            var portletId = parameters.ContainsKey("PortletClientID") ? parameters["PortletClientID"] : string.Empty;

            return $"SN.ListGrid.getSelectedPaths('{portletId}', this.target)";
        }

        protected string GetIdListMethod()
        {
            var parameters = GetParameters();
            var portletId = parameters.ContainsKey("PortletClientID") ? parameters["PortletClientID"] : string.Empty;

            return $"SN.ListGrid.getSelectedIds('{portletId}', this.target)";
        }

        public override string Callback
        {
            get
            {
                return this.Forbidden ? string.Empty : GetOpenContentPickerScript();
            }
            set
            {
                base.Callback = value;
            }
        } 
    }
}

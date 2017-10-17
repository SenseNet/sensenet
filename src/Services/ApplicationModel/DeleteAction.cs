using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using System.Web;
using SenseNet.Services.ApplicationModel;

namespace SenseNet.ApplicationModel
{
    public class DeleteAction : DeleteBatchAction
    {
        protected override string GetCallBackScript()
        {
            return string.Format(
@"if ($(this).hasClass('sn-disabled')) 
    return false; 
var paths = ['{0}']; 
var ids = [{1}];
var contextpath = '{3}';
var redirectPath = '{4}';
SN.Util.CreateServerDialog('/Root/System/WebRoot/DeleteAction.aspx','{2}', {{paths:paths,ids:ids,contextpath:contextpath,batch:false,redirectPath:redirectPath}});",
                this.Content.Path, 
                this.Content.Id, 
                SenseNetResourceManager.Current.GetString("ContentDelete", "DeleteStatusDialogTitle"),
                this.Content.Path,
                this.RedirectToBackUrl 
                    ? HttpUtility.UrlDecode(this.BackUri)
                    : this.BackUri != null && this.BackUri.Contains("action=Explore")
                        ? this.Content.ContentHandler.ParentPath + "?action=Explore"
                        : string.Empty
                );
        }

        public override bool IsODataOperation { get { return true; } }
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("permanent", typeof(bool)) };

        public override object Execute(Content content, params object[] parameters)
        {
            var permanent = parameters != null && parameters.Length > 0 && parameters[0] != null && (bool)parameters[0];
            content.Delete(permanent);
            return null;
        }
    }
}

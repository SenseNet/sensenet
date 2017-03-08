using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.i18n;
using System;
using System.Linq;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.ApplicationModel
{
    public class DeleteBatchAction : ClientAction
    {
        public override string Callback
        {
            get
            {
                return this.Forbidden ? string.Empty : string.Format("{0};", GetCallBackScript());
            }
            set
            {
                base.Callback = value;
            }
        }

        private string _portletClientId;
        public string PortletClientId
        {
            get 
            { 
                return _portletClientId ?? (_portletClientId = GetPortletClientId());
            }
        }
        protected string GetPortletClientId()
        {
            var parameters = GetParameteres();
            return parameters.ContainsKey("PortletClientID") ? parameters["PortletClientID"].ToString() : string.Empty;
        }

        protected virtual string GetCallBackScript()
        {
            var redirectPath = string.Empty;
            if (PortalContext.Current.IsOdataRequest)
            {
                // In case of an OData request (which is likely a request for actions) we can 
                // use the given back url or the referrer. The latter will contain the page 
                // where the ajax request was initiated (e.g. an Explore or browse page).
                redirectPath = PortalContext.Current.BackUrl;
                if (string.IsNullOrEmpty(redirectPath) &&
                    PortalContext.Current.OwnerHttpContext.Request.UrlReferrer != null)
                    redirectPath = PortalContext.Current.OwnerHttpContext.Request.UrlReferrer.PathAndQuery;
            }
            else
            {
                // original behavior for webforms action controls
                if (PortalContext.Current.ActionName == "Explore")
                    redirectPath =  this.Content.Path + "?action=Explore";
            }

            return string.Format(
@"if ($(this).hasClass('sn-disabled')) 
    return false; 
var paths = SN.ListGrid.getSelectedPaths('{0}', this); 
var ids = SN.ListGrid.getSelectedIdsList('{0}', this); 
var contextpath = '{2}';
var redirectPath = '{3}';
SN.Util.CreateServerDialog('/Root/System/WebRoot/DeleteAction.aspx','{1}', {{paths:paths,ids:ids,contextpath:contextpath,batch:true,redirectPath:redirectPath}});",
                PortletClientId, 
                SenseNetResourceManager.Current.GetString("ContentDelete", "DeleteStatusDialogTitle"),
                this.Content.Path,
                redirectPath
                );
        }

        public override bool IsODataOperation => true;

        public override ActionParameter[] ActionParameters { get; } =
        {
            new ActionParameter("paths", typeof (object[]), true),
            new ActionParameter("permanent", typeof (bool))
        };

        public override object Execute(Content content, params object[] parameters)
        {
            var permanent = parameters.Length > 1 && parameters[1] != null && (bool)parameters[1];
            var exceptions = new List<Exception>();

            // no need to throw an exception if no ids are provided: we simply do not have to delete anything
            var ids = parameters[0] as object[];
            if (ids == null)
                return null;

            foreach (var node in Node.LoadNodes(ids.Select(NodeIdentifier.Get)))
            {
                try
                {
                    var gc = node as GenericContent;
                    if (gc != null)
                    {
                        gc.Delete(permanent);
                    }
                    else
                    {
                        var ct = node as ContentType;
                        ct?.Delete();
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);

                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);
                }
            }
            if (exceptions.Count > 0)
                throw new Exception(string.Join(Environment.NewLine, exceptions.Select(e => e.Message)));

            return null;
        }
    }
}

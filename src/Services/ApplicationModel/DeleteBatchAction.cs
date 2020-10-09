using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Portal.OData;
using SenseNet.Portal.Virtualization;

namespace SenseNet.ApplicationModel
{
    public class DeleteBatchAction : ClientAction
    {
        public override string Callback
        {
            get => this.Forbidden ? string.Empty : $"{GetCallBackScript()};";
            set => base.Callback = value;
        }

        private string _portletClientId;
        public string PortletClientId => _portletClientId ?? (_portletClientId = GetPortletClientId());

        protected string GetPortletClientId()
        {
            var parameters = GetParameters();
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
                    redirectPath = this.Content.Path + "?action=Explore";
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
            // no need to throw an exception if no ids are provided: we simply do not have to delete anything
            if (!(parameters[0] is object[] ids))
                return null;

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = ids.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifier>();
            var nodes = Node.LoadNodes(identifiers);

            foreach (var node in nodes)
            {
                try
                {
                    // Collect already found identifiers in a separate list otherwise the error list
                    // would contain multiple errors for the same content.
                    foundIdentifiers.Add(NodeIdentifier.Get(node));

                    switch (node)
                    {
                        case GenericContent gc:
                            gc.Delete(permanent);
                            break;
                        case ContentType ct:
                            ct.Delete();
                            break;
                    }

                    results.Add(new { node.Id, node.Path, node.Name });
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);

                    errors.Add(new ErrorContent
                    {
                        Content = new {node?.Id, node?.Path},
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo {Trace = e.StackTrace},
                            Message = new ErrorMessage
                            {
                                Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                                Value = e.Message
                            }
                        }
                    });
                }
            }

            // iterating through the missing identifiers and making error items for them
            errors.AddRange(identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path))
                .Select(missing => new ErrorContent
                {
                    Content = new {missing?.Id, missing?.Path},
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage
                        {
                            Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound),
                                missing?.Path)
                        }
                    }
                }));

            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }
    }
}
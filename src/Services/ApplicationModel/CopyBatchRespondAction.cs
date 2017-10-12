using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Portal.OData;

namespace SenseNet.Services.ApplicationModel
{
    public class CopyBatchRespondAction : CopyToAction
    {
        protected override string GetCallBackScript()
        {
            return GetServiceCallBackScript(
                url: ODataTools.GetODataOperationUrl(Content, "CopyBatch", true),
                scriptBeforeServiceCall: "var paths = " + GetPathListMethod(),
                postData: "JSON.stringify({ targetPath: targetPath, paths: paths })",
                inprogressTitle: SenseNetResourceManager.Current.GetString("Action", "CopyInProgressDialogTitle"),
                successContent: SenseNetResourceManager.Current.GetString("Action", "CopyDialogContent"),
                successTitle: SenseNetResourceManager.Current.GetString("Action", "CopyDialogTitle"),
                successCallback: @"SN.Util.RefreshExploreTree([targetPath]);",
                errorCallback: @"SN.Util.RefreshExploreTree([targetPath]);",
                successCallbackAfterDialog: "location = location;",
                errorCallbackAfterDialog: "location = location;"
            );
        }

        // =========================================================================== OData

        public override bool IsODataOperation { get { return true; } }

        public override ActionParameter[] ActionParameters { get; } =
        {
            new ActionParameter("targetPath", typeof (string), true),
            new ActionParameter("paths", typeof (object[]), true)
        };

        public override object Execute(Content content, params object[] parameters)
        {
            var targetPath = (string)parameters[0];
            var targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new ContentNotFoundException(targetPath);

            var ids = parameters[1] as object[];
            if (ids == null)
            {
                throw new InvalidOperationException("No content identifiers provided.");
            }
            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = ids.Select(NodeIdentifier.Get).ToList();
            var nodes = Node.LoadNodes(identifiers).ToList();
            foreach (var id in identifiers)
            {
                var node = nodes.FirstOrDefault(n => n.Id == id.Id || n.Path == id.Path);
                if (node == null)
                {
                    errors.Add(new ErrorContent
                    {
                        Content = new { id.Id, id.Path, Name = "" },
                        Error = new Error
                        {
                            Code = "InvalidId",
                            ExceptionType = null
                            ,InnerError = null
                            ,Message = new ErrorMessage { Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(), Value = null }
                        }
                    });
                    continue;
                }
                try
                {
                    node.CopyTo(targetNode);
                    results.Add(new { node.Id, node.Path, node.Name});
                }
                catch (Exception e)
                {
                    //TODO: we should log only relevant exceptions here and skip
                    // business logic-related errors, e.g. lack of permissions or
                    // existing target content path.
                    SnLog.WriteException(e);
                    errors.Add(new ErrorContent {Content = new {node.Id, node.Path, node.Name}, Error = new Error
                    {
                        Code = "NotSpecified", ExceptionType = e.GetType().FullName
                        , InnerError = new StackInfo { Trace = e.StackTrace}
                        , Message = new ErrorMessage { Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(), Value = e.Message}
                    }});
                }
            }
            return ODataActionResponse.Create(results, errors, results.Count + errors.Count);
        }
    }
}
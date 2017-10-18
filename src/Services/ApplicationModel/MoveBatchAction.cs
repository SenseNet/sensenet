using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Portal;
using SenseNet.Portal.OData;
using SenseNet.Services.ApplicationModel;

namespace SenseNet.ApplicationModel
{
    public class MoveBatchAction : MoveToAction
    {
        protected override string GetCallBackScript()
        {
            return GetServiceCallBackScript(
                url: ODataTools.GetODataOperationUrl(Content, "MoveBatch", true),
                scriptBeforeServiceCall: "var paths = " + GetPathListMethod(),
                postData: "JSON.stringify({ targetPath: targetPath, paths: paths })",
                inprogressTitle: SenseNetResourceManager.Current.GetString("Action", "MoveInProgressDialogTitle"),
                successContent: SenseNetResourceManager.Current.GetString("Action", "MoveDialogContent"),
                successTitle: SenseNetResourceManager.Current.GetString("Action", "MoveDialogTitle"),
                successCallback: @"
var pathToRefresh = SN.Util.GetParentPath(paths[0]);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                errorCallback: @"
var pathToRefresh = SN.Util.GetParentPath(paths[0]);
SN.Util.RefreshExploreTree([pathToRefresh, targetPath]);",
                successCallbackAfterDialog: "location=location;",
                errorCallbackAfterDialog: "location=location;"
                );
        }

        public override bool IsODataOperation => true;

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
                throw new InvalidOperationException("No content identifiers provided.");

            var results = new List<object>();
            var errors = new List<ErrorContent>();
            var identifiers = ids.Select(NodeIdentifier.Get).ToList();
            var foundIdentifiers = new List<NodeIdentifierValue>();
            var nodes = Node.LoadNodes(identifiers);
            foreach (var node in nodes)
            {
                try
                {
                    foundIdentifiers.Add(new NodeIdentifierValue { Id = node.Id, Path = node.Path });
                    node.MoveTo(targetNode);
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
                        Content = new {node?.Id, node?.Path, node?.Name },
                        Error = new Error
                        {
                            Code = "NotSpecified",
                            ExceptionType = e.GetType().FullName,
                            InnerError = new StackInfo { Trace = e.StackTrace },
                            Message = new ErrorMessage { Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(), Value = e.Message }
                        }
                    });
                }
            }
            // iterating through the missing identifiers and making error items for them
            foreach (var missing in identifiers.Where(id => !foundIdentifiers.Exists(f => f.Id == id.Id || f.Path == id.Path)))
            {
                errors.Add(new ErrorContent
                {
                    Content = new { missing?.Id, missing?.Path },
                    Error = new Error
                    {
                        Code = "ResourceNotFound",
                        ExceptionType = "ContentNotFoundException",
                        InnerError = null,
                        Message = new ErrorMessage { Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                            Value = string.Format(SNSR.GetString(SNSR.Exceptions.OData.ErrorContentNotFound), missing?.Path)
                        }

                    }
                });
            }
            return BatchActionResponse.Create(results, errors, results.Count + errors.Count);
        }

    }
}
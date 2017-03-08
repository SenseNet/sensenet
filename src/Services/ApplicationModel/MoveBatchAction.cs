using SenseNet.ContentRepository.i18n;
using System;
using SenseNet.ContentRepository;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using System.Linq;
using SenseNet.Portal.OData;

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
            var exceptions = new List<Exception>();

            var targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new ContentNotFoundException(targetPath);

            var ids = parameters[1] as object[];
            if (ids == null)
                throw new InvalidOperationException("No content identifiers provided.");

            foreach (var node in Node.LoadNodes(ids.Select(NodeIdentifier.Get)))
            {
                try
                {
                    node?.MoveTo(targetNode);
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

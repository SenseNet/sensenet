using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that performs a reject operation on a content.
    /// </summary>
    public sealed class RejectAction : UrlAction
    {
        public override bool IsHtmlOperation => false;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange => true;
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("rejectReason", typeof(string), false) };

        public override object Execute(Content content, params object[] parameters)
        {
            return ContentOperations.Reject(content, parameters.FirstOrDefault() as string);
        }
    }
}

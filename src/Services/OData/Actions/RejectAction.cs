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
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't reject content '{content.Path}' because its content handler is not a GenericContent. It needs to inherit from GenericContent for collaboration feature support.");

            var rejectReason = parameters.FirstOrDefault() as string ?? string.Empty;

            content["RejectReason"] = rejectReason;
            content.Reject();

            return content;
        }
    }
}

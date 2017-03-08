using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that performs a check-in operation on a content, using the given check-in comments.
    /// </summary>
    public sealed class CheckInAction : UrlAction
    {
        public override bool IsODataOperation { get; } = true;
        public override bool IsHtmlOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("checkInComments", typeof(string), false), };

        public override object Execute(Content content, params object[] parameters)
        {
            var checkInComments = parameters.FirstOrDefault() as string ?? string.Empty;

            if (string.IsNullOrEmpty(checkInComments) && content.CheckInCommentsMode == CheckInCommentsMode.Compulsory)
                throw new Exception($"Can't check in content '{content.Path}' without checkin comments because its CheckInCommentsMode is set to CheckInCommentsMode.Compulsory.");
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't check in content '{content.Path}' because its content handler is not a GenericContent. It needs to inherit from GenericContent for collaboration feature support.");

            content["CheckInComments"] = checkInComments;
            content.CheckIn();

            return content;
        }
    }
}

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
            return ContentOperations.CheckIn(content, parameters.FirstOrDefault() as string);
        }
    }
}

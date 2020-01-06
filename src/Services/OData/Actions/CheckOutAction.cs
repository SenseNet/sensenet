using System;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that performs a check-out operation on a content.
    /// </summary>
    public sealed class CheckOutAction : UrlAction
    {
        public override bool IsODataOperation { get; } = true;
        public override bool IsHtmlOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;

        public override object Execute(Content content, params object[] parameters)
        {
            return ContentOperations.CheckOut(content);
        }
    }
}

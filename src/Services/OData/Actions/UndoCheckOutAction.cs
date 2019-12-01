using System;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that undoes the check-out on a content.
    /// </summary>
    public sealed class UndoCheckOutAction : UrlAction
    {
        public override bool IsHtmlOperation { get; } = true;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;

        public override object Execute(Content content, params object[] parameters)
        {
            return ContentOperations.UndoCheckOut(content);
        }
    }
}

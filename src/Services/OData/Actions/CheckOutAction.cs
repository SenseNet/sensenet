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
            if (!(content.ContentHandler is GenericContent))
                throw new Exception($"Can't check out content '{content.Path}' because its content handler is not a GenericContent. It needs to inherit from GenericContent for collaboration feature support.");

            content.CheckOut();

            return content;
        }
    }
}

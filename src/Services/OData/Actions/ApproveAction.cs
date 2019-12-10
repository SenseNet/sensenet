using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that performs an approve operation on a content.
    /// </summary>
    public sealed class ApproveAction : UrlAction
    {
        public sealed override bool IsHtmlOperation { get { return true; } }
        public sealed override bool IsODataOperation { get { return true; } }
        public sealed override bool CausesStateChange { get { return true; } }

        public sealed override object Execute(Content content, params object[] parameters)
        {
            return ContentOperations.Approve(content);
        }
    }
}

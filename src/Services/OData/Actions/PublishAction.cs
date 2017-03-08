using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that performs a publish operation on a content.
    /// </summary>
    public sealed class PublishAction : UrlAction
    {
        public sealed override bool IsHtmlOperation { get { return true; } }
        public sealed override bool IsODataOperation { get { return true; } }
        public sealed override bool CausesStateChange { get { return true; } }

        public sealed override object Execute(Content content, params object[] parameters)
        {
            // Perform checks
            if (!(content.ContentHandler is GenericContent))
                throw new Exception(string.Format("Can't publish content '{0}' because its content handler is not a GenericContent. It needs to inherit from GenericContent for collaboration feature support.", content.Path));

            // Do the action
            content.Publish();

            // Return actual state of content
            return content;
        }
    }
}

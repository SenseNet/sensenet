using System;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.Portal.ApplicationModel.AspectActions
{
    public sealed class RetrieveFieldsAction : ActionBase
    {
        public override string Uri { get; } = null;
        public override bool IsHtmlOperation { get; } = false;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;

        public override object Execute(Content content, params object[] parameters)
        {
            var aspect = content.ContentHandler as Aspect;
            if (aspect == null)
                throw new InvalidOperationException("This action only works with Aspect content items.");

            var result = aspect.FieldSettings.Select(x => x.ToFieldInfo()).ToArray();
            return result;
        }
    }
}

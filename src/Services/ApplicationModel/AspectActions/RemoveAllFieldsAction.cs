using System;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class RemoveAllFieldsAction : AspectActionBase
    {
        public override ActionParameter[] ActionParameters => ActionParameter.EmptyParameters;

        public override object Execute(Content content, params object[] parameters)
        {
            var aspect = content.ContentHandler as Aspect;
            if (aspect == null)
                throw new InvalidOperationException("Cannot remove Fields from a content that is not an Aspect.");

            aspect.RemoveAllfields();
            return null;
        }
    }
}

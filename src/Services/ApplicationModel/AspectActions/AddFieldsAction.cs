using System;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class AddFieldsAction : AspectActionBase
    {
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("fields", typeof(FieldInfo[]), true) };

        public override object Execute(Content content, params object[] parameters)
        {
            var fieldInfos = (FieldInfo[])parameters[0];
            var aspect = content.ContentHandler as Aspect;
            if (aspect == null)
                throw new InvalidOperationException("Cannot add Field to a content that is not an Aspect.");

            aspect.AddFields(fieldInfos);
            return null;
        }
    }
}

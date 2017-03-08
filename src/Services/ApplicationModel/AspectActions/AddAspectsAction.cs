using System;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class AddAspectsAction : AspectActionBase
    {
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("aspects", typeof(string[]), true) };

        public override object Execute(Content content, params object[] parameters)
        {
            var aspectPaths = (string[])parameters[0];
            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
                throw new InvalidOperationException("Cannot add an aspect to a content that is not a GenericContent.");
            var aspects = new Aspect[aspectPaths.Length];
            for (int i = 0; i < aspectPaths.Length; i++)
            {
                var pathOrName = aspectPaths[i];
                aspects[i] = Aspect.LoadAspectByPathOrName(pathOrName);
                if (aspects[i] == null)
                    throw new InvalidOperationException("Unknown aspect: " + aspectPaths[i]);
            }
            content.AddAspects(aspects);
            content.Save();
            return null;
        }
    }
}

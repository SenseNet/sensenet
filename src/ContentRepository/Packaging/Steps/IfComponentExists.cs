using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging.Steps
{
    public class IfComponentExists : ConditionalStep
    {
        public IEnumerable<XmlElement> Components { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (Components == null)
                return true;

            foreach (var dependency in Components.Select(Dependency.Parse))
            {
                try
                {
                    context.Manifest.CheckDependency(dependency, RepositoryVersionInfo.Instance, false);
                }
                catch (PackagePreconditionException)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

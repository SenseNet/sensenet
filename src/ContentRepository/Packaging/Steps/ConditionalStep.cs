using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SenseNet.Packaging.Steps
{
    public abstract class ConditionalStep : Step
    {
        public IEnumerable<XmlElement> Then { get; set; }
        public IEnumerable<XmlElement> Else { get; set; }

        protected abstract bool EvaluateCondition(ExecutionContext context);

        public override void Execute(ExecutionContext context)
        {
            var childSteps = ((EvaluateCondition(context) ? Then : Else) ?? new List<XmlElement>()).ToList();
            PackageManager.ExecuteSteps(childSteps, context);
        }
    }

    // preview :)
    internal abstract class While : Step
    {
        public IEnumerable<XmlElement> Block { get; set; }

        protected abstract bool EvaluateCondition(ExecutionContext context);

        public override void Execute(ExecutionContext context)
        {
            while (EvaluateCondition(context))
                PackageManager.ExecuteSteps(Block.ToList(), context);
        }
    }
}

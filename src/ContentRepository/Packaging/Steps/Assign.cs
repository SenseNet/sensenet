using SenseNet.Packaging.Steps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Packaging;

namespace SenseNet.Packaging.Steps
{
    public class Assign : Step
    {
        [DefaultProperty]
        public string Value { get; set; }

        public string Name { get; set; }
        public override void Execute(ExecutionContext context)
        {
            SetVariable(Name, Value, context);
            context.Console.WriteLine($"var {Name} = {Value}");
        }
    }
}

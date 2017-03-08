using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Packaging.Steps;

namespace SenseNet.Packaging.Steps
{
    [Annotation("Writes the value of the Text property to the console.")]
    public class Trace : Step
    {
        [DefaultProperty]
        [Annotation("Runtime information that will be displayed.")]
        public string Text { get; set; }

        public override void Execute(SenseNet.Packaging.ExecutionContext context)
        {
            Logger.LogMessage(context.ResolveVariable(this.Text)?.ToString() ?? "[null]");
        }
    }
}

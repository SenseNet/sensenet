using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Packaging.Steps
{
    [Serializable]
    public class PackageTerminatedException : ApplicationException
    {
        public PackageTerminatedException() { }
        public PackageTerminatedException(string message) : base(message) { }
        public PackageTerminatedException(string message, Exception inner) : base(message, inner) { }
        protected PackageTerminatedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
    public class Terminate : Step
    {
        [DefaultProperty]
        public string Message { get; set; }

        public TerminationReason Reason { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.TerminateExecution(this.Message, this.Reason, this);
        }
    }
}

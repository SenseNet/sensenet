using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Packaging.Steps.Internal
{
    internal class SafeQueries
    {
        /// <summary>Returns with the following query: '+TypeIs:Workflow +WorkflowStatus:(Aborted Completed)
        /// -WorkflowInstanceGuid:"00000000-0000-0000-0000-000000000000"'</summary>
        public static string ConnectedAbortedAndCompletedWorkflows
        {
            get { return "+TypeIs:Workflow +WorkflowStatus:(Aborted Completed) -WorkflowInstanceGuid:\"00000000-0000-0000-0000-000000000000\""; }
        }

    }
}

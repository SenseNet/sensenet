using SenseNet.Search;

namespace SenseNet.Packaging.Steps.Internal
{
    internal class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: '+TypeIs:Workflow +WorkflowStatus:(Aborted Completed)
        /// -WorkflowInstanceGuid:"00000000-0000-0000-0000-000000000000"'</summary>
        public static string ConnectedAbortedAndCompletedWorkflows => "+TypeIs:Workflow +WorkflowStatus:(Aborted Completed) -WorkflowInstanceGuid:\"00000000-0000-0000-0000-000000000000\"";

        /// <summary>Returns the following query: +TypeIs:@0 +Locked:true</summary>
        public static string LockedContent => "+TypeIs:@0 +Locked:true";
    }
}

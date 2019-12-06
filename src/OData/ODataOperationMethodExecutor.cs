using System.Threading.Tasks;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    /// <summary>
    /// This action is used when an action is called by the client and the OData module
    /// needs to find and execute the appropriate method.
    /// </summary>
    internal class ODataOperationMethodExecutor : ActionBase
    {
        public override string Uri { get; } = string.Empty;
        public override bool IsHtmlOperation => false;
        public override bool IsODataOperation => true;
        public override bool CausesStateChange => Method.Operation.CausesStateChange;

        public OperationCallingContext Method { get; }
        public bool IsAsync => Method.Operation.IsAsync;

        public ODataOperationMethodExecutor(OperationCallingContext context)
        {
            Method = context;
        }

        public override object Execute(Content content, params object[] parameters)
        {
            return OperationCenter.Invoke(Method);
        }

        public override Task<object> ExecuteAsync(Content content, params object[] parameters)
        {
            return OperationCenter.InvokeAsync(Method);
        }
    }
}

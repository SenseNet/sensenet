using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    public class ODataOperationMethodAction : ActionBase
    {
        internal OperationInfo OperationInfo { get; }
        public override string Uri { get; }
        public override bool IsHtmlOperation => false;
        public override bool IsODataOperation => true;
        public override bool CausesStateChange { get; }
        public override ActionParameter[] ActionParameters { get; }

        public ODataOperationMethodAction(OperationInfo operationInfo, string uri)
        {
            OperationInfo = operationInfo;
            Uri = uri;

            ActionParameters = operationInfo.Method.GetParameters()
                .Skip(1) // Ignore the Content parameter
                .Select(x => new ActionParameter(x.Name, x.ParameterType, !x.IsOptional))
                .ToArray();

            CausesStateChange = operationInfo.CauseStateChange;
        }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);
            Name = OperationInfo.Method.Name;
        }
    }
}

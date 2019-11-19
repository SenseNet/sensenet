using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    public class ODataOperationMethodAction : ActionBase
    {
        public override string Uri { get; } //UNDONE:? never used
        public override bool IsHtmlOperation => false;
        public override bool IsODataOperation => true;
        public override bool CausesStateChange { get; }
        public override ActionParameter[] ActionParameters { get; }
        public override string Name { get; set; }

        public ODataOperationMethodAction(OperationInfo operationInfo)
        {
            Name = operationInfo.Method.Name;

            ActionParameters = operationInfo.Method.GetParameters()
                .Select(x => new ActionParameter(x.Name, x.ParameterType, !x.IsOptional))
                .ToArray();

            CausesStateChange = operationInfo.CauseStateChange;
        }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);
        }
    }
}

using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;

namespace SenseNet.OData
{
    /// <summary>
    /// This action is used when we need to serve a list of actions to the client. This action
    /// is not executed directly, only serves as a metadata holder.
    /// </summary>
    internal class ODataOperationMethodDescriptor : ActionBase
    {
        internal OperationInfo OperationInfo { get; }
        public override string Uri { get; }
        public override bool IsHtmlOperation => false;
        public override bool IsODataOperation => true;
        public override bool CausesStateChange { get; }
        public override ActionParameter[] ActionParameters { get; }

        public string DisplayName
        {
            get => Text ?? OperationInfo.DisplayName;
            set => Text = value;
        }

        private string _icon;
        public override string Icon
        {
            get => _icon ?? OperationInfo.Icon;
            set => _icon = value;
        }

        private string _description;
        public override string Description
        {
            get => _description ?? OperationInfo.Description;
            set => _description = value;
        }

        public ODataOperationMethodDescriptor(OperationInfo operationInfo, string uri)
        {
            OperationInfo = operationInfo;
            Uri = uri;

            ActionParameters = operationInfo.Method.GetParameters()
                .Skip(1) // Ignore the Content parameter
                .Select(x => new ActionParameter(x.Name, x.ParameterType, !x.IsOptional))
                .ToArray();

            CausesStateChange = operationInfo.CausesStateChange;
        }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);
            Name = OperationInfo.Name;
            DisplayName = OperationInfo.DisplayName;
        }
    }
}

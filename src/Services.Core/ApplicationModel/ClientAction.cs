using System;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel
{
    public class ClientAction : ParameterizedAction
    {
        public override string Uri => string.Empty;

        public override bool CausesStateChange => false;
        public override bool IsODataOperation => false;
        public override bool IsHtmlOperation => false;

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            if (!(application is ClientApplication))
                throw new InvalidOperationException("Not a client application: " + application.GetType().Name);

            base.Initialize(context, backUri, application, parameters);
        }
        protected override string GetParametersText()
        {
            return ((ClientApplication)GetApplication()).Parameters;
        }
    }
}

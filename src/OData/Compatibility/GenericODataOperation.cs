using System;
using System.Linq;
using System.Reflection;
using SenseNet.Portal.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.OData;
using SenseNet.Tools;

namespace SenseNet.ApplicationModel
{
    public class GenericODataOperation : ParameterizedAction
    {
        public override string Uri
        {
            get
            {
                if (this.Forbidden)
                    return string.Empty;

                var s = SerializeParameters(GetParameteres());
                var uri = $"/{ODataTools.GetODataUrl(Content.Path).TrimStart('/')}/{this.GetApplication().Name}";

                if (!string.IsNullOrEmpty(s))
                {
                    uri += (uri.Contains("?") ? "&" : "?");
                    uri += s.Substring(1);
                }

                return uri;
            }
        }

        public override bool IsHtmlOperation => false;
        public override bool IsODataOperation => true;
        private bool _causesStateChange = true;
        public override bool CausesStateChange => _causesStateChange;

        private MethodBase _method;
        private bool _hasOperationAttribute;

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            if (!(application is GenericODataApplication))
                throw new InvalidOperationException("Not an OData application: " + application.GetType().Name);
            
            base.Initialize(context, backUri, application, parameters);

            var operationAttribute = (ODataOperationAttribute)_method
                .GetCustomAttributes(typeof(ODataOperationAttribute), true)
                .FirstOrDefault();
            _hasOperationAttribute = operationAttribute != null;
            _causesStateChange = operationAttribute?.CauseStateChange ?? false;
        }

        protected override string GetParametersText()
        {
            return ((GenericODataApplication) GetApplication()).Parameters;
        }

        protected override ActionParameter[] GetActionParameters()
        {
            var app = (GenericODataApplication)GetApplication();
            var type = TypeResolver.GetType(app.ClassName, false);
            if (type == null)
                throw new InvalidOperationException("Unknown type: " + app.ClassName);

            var prmTypes = new Type[ParamTypes.Length + 1];
            prmTypes[0] = typeof(Content);

            Array.Copy(ParamTypes, 0, prmTypes, 1, ParamTypes.Length);

            _method = type.GetMethod(app.MethodName, prmTypes);
            if (_method == null)
                throw new InvalidOperationException("Unknown method: " + app.MethodName);

            var actionParams = new ActionParameter[ParamTypes.Length];
            var actualParameters = _method.GetParameters();
            for (var i = 0; i < ParamTypes.Length; i++)
            {
                actionParams[i] = new ActionParameter(ParamNames[i], ParamTypes[i], !actualParameters[i + 1].HasDefaultValue);
            }

            return actionParams;
        }

        public override object Execute(Content content, params object[] parameters)
        {
            if (!_hasOperationAttribute)
                throw new MethodAccessException("Access denied. This method cannot be called through a generic operation.");

            var p = new object[parameters.Length + 1];
            p[0] = content;
            Array.Copy(parameters, 0, p, 1, parameters.Length);

            try
            {
                return _method.Invoke(null, p);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;

                throw;
            }
        }
    }
}

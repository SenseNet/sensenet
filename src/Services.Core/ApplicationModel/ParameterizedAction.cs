using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tools;

namespace SenseNet.ApplicationModel
{
    public abstract class ParameterizedAction : ActionBase
    {
        private static readonly string[] EmptyStringArray = Array.Empty<string>();
        private static readonly Type[] EmptyTypeArray = Array.Empty<Type>();
        
        protected Type[] ParamTypes;
        protected string[] ParamNames;
        private ActionParameter[] _actionParameters;

        public override string Uri { get; } = string.Empty;
        public override ActionParameter[] ActionParameters => _actionParameters;

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);
            
            ParamTypes = GetMethodParams(GetParametersText(), out var paramNames);
            ParamNames = paramNames;

            _actionParameters = GetActionParameters();
        }

        /// <summary>
        /// Gets parameter list in a comma-separated text.
        /// </summary>
        protected abstract string GetParametersText();
        
        protected virtual ActionParameter[] GetActionParameters()
        {
            var actionParams = new ActionParameter[ParamTypes.Length];
            for (var i = 0; i < ParamTypes.Length; i++)
            {
                actionParams[i] = new ActionParameter(ParamNames[i], ParamTypes[i]);
            }

            return actionParams;
        }

        private static Type[] GetMethodParams(string prms, out string[] prmNames)
        {
            if (string.IsNullOrEmpty(prms))
            {
                prmNames = EmptyStringArray;
                return EmptyTypeArray;
            }

            var prmdefs = prms.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var types = new Type[prmdefs.Length];
            var names = new List<string>();
            for (int i = 0; i < prmdefs.Length; i++)
            {
                var prmDef = prmdefs[i].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var typeName = prmDef[0].Trim();
                var name = prmDef[1].Trim();
                Type type;
                switch (typeName)
                {
                    case "string": type = typeof(string); break;
                    case "int": type = typeof(int); break;
                    case "bool": type = typeof(bool); break;
                    case "long": type = typeof(long); break;
                    case "decimal": type = typeof(decimal); break;
                    case "double": type = typeof(double); break;
                    case "object": type = typeof(object); break;

                    case "byte": type = typeof(byte); break;
                    case "sbyte": type = typeof(sbyte); break;
                    case "char": type = typeof(char); break;
                    case "float": type = typeof(float); break;
                    case "uint": type = typeof(uint); break;
                    case "ulong": type = typeof(ulong); break;
                    case "short": type = typeof(short); break;
                    case "ushort": type = typeof(ushort); break;

                    case "DateTime": type = typeof(DateTime); break;

                    case "string[]": type = typeof(string[]); break;
                    case "int[]": type = typeof(int[]); break;

                    case "Node": type = typeof(Node); break;
                    case "Content": type = typeof(Content); break;
                    default: type = TypeResolver.GetType(typeName); break;
                }
                if (type == null)
                    throw new InvalidOperationException("Unknown parameter type: " + prmDef[0]);
                types[i] = type;

                if (names.Contains(name))
                    throw new InvalidOperationException("duplicated parameter name: " + prmDef[1]);

                types[i] = type;
                names.Add(name);
            }
            prmNames = names.ToArray();

            return types;
        }
    }
}

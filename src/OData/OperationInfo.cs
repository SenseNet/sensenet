using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SenseNet.ApplicationModel;

namespace SenseNet.OData
{
    [DebuggerDisplay("{ToString()}")]
    public class OperationInfo
    {
        public string Name { get; }
        public MethodBase Method { get; }
        public bool IsAsync { get; }

        public string[] RequiredParameterNames { get; set; }
        public Type[] RequiredParameterTypes { get; set; }
        public string[] OptionalParameterNames { get; set; }
        public Type[] OptionalParameterTypes { get; set; }
        public Attribute[] Attributes { get; }

        public bool CausesStateChange { get; private set; }
        public string[] ContentTypes { get; private set; }
        public string[] Permissions { get; private set; }
        public string[] Scenarios { get; private set; }
        public string[] Roles { get; private set; }
        public string[] Policies { get; private set; }

        public OperationInfo(string name, MethodBase method, Attribute[] attributes)
        {
            Name = name;
            Method = method;
            Attributes = attributes;
            ParseAttributes(attributes);
            IsAsync = ParseSynchronicity(method);
        }

        private void ParseAttributes(Attribute[] attributes)
        {
            CausesStateChange = attributes.Any(a => a is ODataAction);

            ContentTypes = ParseNames(attributes
                .Where(a => a is ContentTypeAttribute)
                .SelectMany(a => ((ContentTypeAttribute) a).Names));

            Scenarios = ParseNames(attributes
                .Where(a => a is ScenarioAttribute)
                .Select(a => ((ScenarioAttribute) a).Name));

            if (attributes.Any(a => a is SnAuthorizeAllAttribute))
            {
                Roles = new[] {"All"};
                Policies = _empty;
                Permissions = _empty;
            }
            else
            {
                var snAuthorizeAttributes = attributes.Where(a => a is SnAuthorizeAttribute)
                    .Cast<SnAuthorizeAttribute>().ToArray();
                Roles = ParseNames(snAuthorizeAttributes.Select(a => a.Role));
                Policies = ParseNames(snAuthorizeAttributes.Select(a => a.Policy));
                Permissions = ParseNames(snAuthorizeAttributes.Select(a => a.Permission));
            }
        }

        private static readonly char[] SplitChars = new[] { ',' };
        private string[] ParseNames(IEnumerable<string> sources)
        {
            if (sources == null)
                return _empty;
            var result = new List<string>();
            foreach (var names in sources)
            {
                if (string.IsNullOrEmpty(names))
                    continue;
                result.AddRange(names.Trim()
                    .Split(SplitChars, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()));
            }
            return result.Distinct().ToArray();
        }

        private readonly string[] _empty = new string[0];
        public override string ToString()
        {
            var code = new List<string>();
            if (RequiredParameterNames != null)
                for (int i = 0; i < RequiredParameterNames.Length; i++)
                    code.Add($"{TypeToString(RequiredParameterTypes[i])} {RequiredParameterNames[i]}");
            if (OptionalParameterNames != null)
                for (int i = 0; i < OptionalParameterNames.Length; i++)
                    code.Add($"{TypeToString(OptionalParameterTypes[i])} {OptionalParameterNames[i]}?");
            var parameters = string.Join(", ", code);
            return $"{Name}({parameters})";
        }
        private string TypeToString(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int))
                return "int";
            return type.Name;
        }

        private bool ParseSynchronicity(MethodBase methodBase)
        {
            if (!(methodBase is MethodInfo method))
                return false;

            return method.ReturnType == typeof(Task) ||
                   method.ReturnType.BaseType == typeof(Task);
        }
    }
}

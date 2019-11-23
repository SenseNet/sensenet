using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SenseNet.ApplicationModel;

namespace SenseNet.OData
{
    [DebuggerDisplay("{ToString()}")]
    public class OperationInfo
    {
        public MethodBase Method { get; }
        public string[] RequiredParameterNames { get; set; }
        public Type[] RequiredParameterTypes { get; set; }
        public string[] OptionalParameterNames { get; set; }
        public Type[] OptionalParameterTypes { get; set; }
        public Attribute[] Attributes { get; }

        public bool CauseStateChange { get; private set; }
        public string[] ContentTypes { get; private set; }
        public string[] Permissions { get; private set; }
        public string[] Scenarios { get; private set; }
        public string[] Roles { get; private set; }
        public string[] Policies { get; private set; }

        public OperationInfo(MethodBase method, Attribute[] attributes)
        {
            Method = method;
            Attributes = attributes;
            ParseAttributes(attributes);
        }

        private void ParseAttributes(Attribute[] attributes)
        {
            CauseStateChange = attributes.Any(a => a is ODataAction);

            ContentTypes = ParseNames(attributes
                .Where(a => a is ContentTypeAttribute)
                .Select(a => ((ContentTypeAttribute)a).ContentTypeName));

            Scenarios = ParseNames(attributes
                .Where(a => a is ScenarioAttribute)
                .Select(a => ((ScenarioAttribute)a).Name));

            var snAuthorizeAttributes = attributes.Where(a => a is SnAuthorizeAttribute).Cast<SnAuthorizeAttribute>().ToArray();
            Roles = ParseNames(snAuthorizeAttributes.Select(a => a.Role));
            Policies = ParseNames(snAuthorizeAttributes.Select(a => a.Policy));
            Permissions = ParseNames(snAuthorizeAttributes.Select(a => a.Permission));
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
            return $"{Method.Name}({parameters})";
        }

        private string TypeToString(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int))
                return "int";
            return type.Name;
        }
    }
}

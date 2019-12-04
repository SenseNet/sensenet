using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SenseNet.ApplicationModel;

namespace SenseNet.OData
{
    [DebuggerDisplay("{ToString()}")]
    public class OperationInfo
    {
        private string _icon;

        public string Name { get; }
        public string Icon => _icon ?? "Application";
        public string Description { get; }

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

        public OperationInfo(string name, string icon, string description, MethodBase method, Attribute[] attributes)
        {
            Name = name;
            _icon = icon;
            Description = description;
            Method = method;
            Attributes = attributes;

            ParseAttributes(attributes);
            IsAsync = ParseSynchronicity(method);
        }

        private void ParseAttributes(Attribute[] attributes)
        {
            CausesStateChange = attributes.Any(a => a is ODataAction);

            ContentTypes = ParseNames(attributes
                .Where(a => a is ContentTypesAttribute)
                .SelectMany(a => ((ContentTypesAttribute) a).Names));
            if (ContentTypes.Length == 0)
                ContentTypes = new[] {N.GenericContent};
            else if (ContentTypes.Contains(N.GenericContent) && ContentTypes.Contains(N.ContentType))
                ContentTypes = _empty;

            Scenarios = ParseNames(attributes
                .Where(a => a is ScenarioAttribute)
                .Select(a => ((ScenarioAttribute) a).Name));

            Permissions = ParseNames(attributes
                .Where(a => a is RequiredPermissionsAttribute)
                .SelectMany(a => ((RequiredPermissionsAttribute)a).Names));

            Roles = ParseNames(attributes
                .Where(a => a is AllowedRolesAttribute)
                .SelectMany(a => ((AllowedRolesAttribute)a).Names));

            Policies = ParseNames(attributes
                .Where(a => a is RequiredPoliciesAttribute)
                .SelectMany(a => ((RequiredPoliciesAttribute)a).Names));

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

using System.Collections.Generic;
using System.Xml;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;

namespace SenseNet.ContentRepository.Packaging.Steps
{
    [Annotation("Selects a single node defined by the given xpath and stores its value in a varable.")]
    public class SelectXmlValue : Step
    {
        private string _file;

        public string File
        {
            get
            {
                if (string.IsNullOrEmpty(_file))
                    throw new InvalidStepParameterException("Missing 'file' attribute");
                return _file;
            }
            set => _file = value;
        }

        private string _xpath;

        public string Xpath
        {
            get
            {
                if (string.IsNullOrEmpty(_xpath))
                    throw new InvalidStepParameterException("Missing 'xpath' attribute");
                return _xpath;
            }
            set => _xpath = value;
        }

        private string _variableName;

        public string VariableName
        {
            get
            {
                if (string.IsNullOrEmpty(_variableName))
                    throw new InvalidStepParameterException("Missing 'variableName' attribute");
                return _variableName;
            }
            set => _variableName = value;
        }

        public string SubstringBefore { get; set; }
        public string SubstringAfter { get; set; }

        public PathRelativeTo PathIsRelativeTo { get; set; } = PathRelativeTo.TargetDirectory;

        public override void Execute(ExecutionContext context)
        {
            foreach (var path in ResolvePaths(File, PathIsRelativeTo, context))
            {
                var doc = new XmlDocument();
                doc.Load(path);

                var node = doc.SelectSingleNode(Xpath);
                if (node == null)
                    continue;

                var value = (node is XmlElement) ? node.InnerXml : node.Value;
                if (!string.IsNullOrEmpty(SubstringBefore))
                    value = GetSubstringBefore(value, SubstringBefore);
                if (!string.IsNullOrEmpty(SubstringAfter))
                    value = GetSubstringAfter(value, SubstringAfter);

                SetVariable("@" + VariableName, value, context);
            }
        }
        private string GetSubstringBefore(string value, string search)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var p = value.IndexOf(search);
            if (p < 0)
                return value;

            if (p == 0)
                return string.Empty;

            return value.Substring(0, p);
        }
        private string GetSubstringAfter(string value, string search)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var p = value.IndexOf(search);
            if (p < 0)
                return value;

            p += search.Length;
            if (p > value.Length)
                return value;

            return value.Substring(p);
        }

        private IEnumerable<string> ResolvePaths(string path, PathRelativeTo relativeTo, ExecutionContext context)
        {
            var resolvedPath = (string) context.ResolveVariable(path);

            return relativeTo == PathRelativeTo.Package
                ? new[] {ResolvePackagePath(resolvedPath, context)}
                : ResolveAllTargets(resolvedPath, context);
        }

    }
}

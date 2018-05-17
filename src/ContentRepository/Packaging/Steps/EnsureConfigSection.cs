using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SenseNet.Packaging.Steps
{
    [Annotation("Ensures that the given section exists in the configuration files.")]
    public class EnsureConfigSection : Step
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

        private string _sectionPath;
        public string SectionPath
        {
            get
            {
                if (string.IsNullOrEmpty(_sectionPath))
                    throw new InvalidStepParameterException("Missing 'sectionPath' attribute");
                return _sectionPath;
            }
            set => _sectionPath = value;
        }

        public PathRelativeTo PathIsRelativeTo { get; set; } = PathRelativeTo.TargetDirectory;

        public override void Execute(ExecutionContext context)
        {
            foreach (var path in ResolvePaths(File, PathIsRelativeTo, context))
            {
                var doc = new XmlDocument();
                doc.Load(path);

                // check if the original xml contains a declaration header
                var omitXmlDeclaration = doc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault() == null;

                EditConfiguration.CreateSection(doc, SectionPath);

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = omitXmlDeclaration,
                    CloseOutput = true
                };

                using (var writer = XmlWriter.Create(path, settings))
                    doc.Save(writer);
            }
        }
        private IEnumerable<string> ResolvePaths(string path, PathRelativeTo relativeTo, ExecutionContext context)
        {
            var resolvedPath = (string)context.ResolveVariable(path);

            return relativeTo == PathRelativeTo.Package
                ? new[] { ResolvePackagePath(resolvedPath, context) }
                : ResolveAllTargets(resolvedPath, context);
        }

    }
}
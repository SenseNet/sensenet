using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;

namespace SenseNet.Packaging.Steps
{
    public class IfContentExists : ConditionalStep
    {
        public string Path { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            return !string.IsNullOrEmpty(Path) && Node.Exists(Path);
        }
    }

    public class IfFieldExists : ConditionalStep
    {
        public string Field { get; set; }
        public string ContentType { get; set; }
        public bool LocalOnly { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(ContentType) || string.IsNullOrEmpty(Field))
                throw new PackagingException("ContentType or Field is empty.");

            var ct = ContentRepository.Schema.ContentType.GetByName(ContentType);
            if (ct == null)
                throw new PackagingException("ContentType not found: " + ContentType);

            // We have to check only this particular CTD xml, it does not 
            // matter if the field is already defined in a parent type.
            if (LocalOnly)
            {
                var xDoc = EditContentType.LoadContentTypeXmlDocument(ct);
                var field = EditContentType.LoadFieldElement(xDoc, Field, false);

                return field != null;
            }

            return ct.FieldSettings.Any(fs => string.Compare(fs.Name, Field, StringComparison.Ordinal) == 0);
        }
    }

    public class IfFileExists : ConditionalStep
    {
        public string Path { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            return !string.IsNullOrEmpty(Path) && System.IO.File.Exists(ResolveTargetPath(Path, context));
        }
    }

    public class IfDirectoryExists : ConditionalStep
    {
        public string Path { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            return !string.IsNullOrEmpty(Path) && System.IO.Directory.Exists(ResolveTargetPath(Path, context));
        }
    }
}

using SenseNet.ContentRepository;
using System.IO;
using System.Linq;

namespace SenseNet.Packaging.Steps.Internal
{
    public class ImportCustomContent : Import
    {
        public ImportCustomContent()
        {
            Source = "Customization\\import";
            SourceIsRelativeTo = PathRelativeTo.Package;
            Target = Repository.RootPath;
        }

        public override void Execute(ExecutionContext context)
        {
            var source = ResolvePackagePath(Source, context);
            if (!Directory.Exists(source) || !Directory.EnumerateFileSystemEntries(source).Any())
            {
                Logger.LogMessage("No custom content to import.");
                return;
            }

            base.Execute(context);
        }
    }
}

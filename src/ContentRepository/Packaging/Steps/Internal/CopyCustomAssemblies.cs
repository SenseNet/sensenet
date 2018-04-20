using System.IO;
using System.Linq;

namespace SenseNet.Packaging.Steps.Internal
{
    public class CopyCustomAssemblies : Copy
    {
        public CopyCustomAssemblies()
        {
            Source = "Customization\\bin";
            SourceIsRelativeTo = PathRelativeTo.Package;

            // This will make the system copy everything from the bin folder above
            // to the web\bin folder of the target application.
            TargetDirectory = ".";
        }

        public override void Execute(ExecutionContext context)
        {
            var source = ResolvePackagePath(Source, context);
            if (!Directory.Exists(source) || !Directory.EnumerateFileSystemEntries(source).Any())
            {
                Logger.LogMessage("No custom assemblies to copy.");
                return;
            }

            base.Execute(context);
        }
    }
}

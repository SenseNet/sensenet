using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;
using IO = System.IO;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Steps
{
    public class Import : ImportBase
    {
        public string Target { get; set; }
        public PathRelativeTo SourceIsRelativeTo { get; set; }
        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();
            try
            {
                string sourcePath;
                if (SourceIsRelativeTo == PathRelativeTo.Package)
                {
                    sourcePath = ResolvePackagePath(Source, context);
                }
                else
                {
                    sourcePath = ResolveTargetPath(Source, context);//  ResolveAllTargets(Source, context);
                }

                if (!IO.Directory.Exists(sourcePath) && !IO.File.Exists(sourcePath))
                    throw new PackagingException(SR.Errors.Import.SourceNotFound + "\nSource value:" + sourcePath);

                var checkResult = RepositoryPath.IsValidPath(Target);
                if (checkResult != RepositoryPath.PathResult.Correct)
                    if (!Target.StartsWith("/root", StringComparison.OrdinalIgnoreCase))
                        throw new PackagingException(SR.Errors.Import.InvalidTarget, RepositoryPath.GetInvalidPathException(checkResult, Target));

                if (!Node.Exists(Target))
                    throw new PackagingException(SR.Errors.Import.TargetNotFound);

                DoImport(null, sourcePath, Target);
            }
            catch(InvalidStepParameterException)
            {
                Logger.LogMessage("Import step can work with valid paths only.");
                throw;
            }
}

    }
}
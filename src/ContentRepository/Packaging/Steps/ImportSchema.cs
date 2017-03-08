using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Packaging;
using IO = System.IO;

namespace SenseNet.Packaging.Steps
{
    public class ImportSchema : ImportBase
    {
        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var sourcePath = ResolvePackagePath(Source, context);
            if (!IO.Directory.Exists(sourcePath) && !IO.File.Exists(sourcePath))
                throw new PackagingException(SR.Errors.Import.SourceNotFound);

            base.DoImport(sourcePath, null, null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using File = System.IO.File;

namespace SenseNet.Packaging.Steps
{
    public class Rename : Step
    {
        public string Source { get; set; }

        public PathRelativeTo SourceIsRelativeTo { get; set; }

        [DefaultProperty]
        public string NewName { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.Console.Write("Starting renaming... ");
            try
            {
                if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(NewName))
                    throw new PackagingException("Invalid parameters");
                var name = ResolveVariable(NewName, context);
                var contextPath = ResolveRepositoryPath(Source, context);
                if (RepositoryPath.IsValidPath(contextPath) == RepositoryPath.PathResult.Correct)
                {
                    context.AssertRepositoryStarted();
                    var content = Content.Load(contextPath);
                    if (content == null)
                    {
                        throw new ContentNotFoundException(contextPath);
                    }
                    content["Name"] = name;
                    content.SaveSameVersion();
                    return;
                }

                // it is not a repository path so it should be a file system path
                if (SourceIsRelativeTo == PathRelativeTo.Package)
                {
                    contextPath = ResolvePackagePath(Source, context);
                    var targetPath = Path.Combine(Path.GetDirectoryName(contextPath), name);
                    File.Move(contextPath, targetPath);
                }
                else
                {
                    var sourcePaths = ResolveAllTargets(Source, context);
                    for (int i = 0; i < sourcePaths.Length; i++)
                    {
                        var targetPath = Path.Combine(Path.GetDirectoryName(sourcePaths[i]), name);
                        File.Move(sourcePaths[i], targetPath);
                    }
                }
                context.Console.WriteLine(name +" renamed all right.");
            }
            catch (InvalidStepParameterException)
            {
                Logger.LogMessage("Rename step can work with valid paths only.");
                throw;
            }
        }
    }
}

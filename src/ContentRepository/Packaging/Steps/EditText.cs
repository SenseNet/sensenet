using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using File = System.IO.File;

namespace SenseNet.Packaging.Steps
{
    /// <summary>
    /// Base class for text editing operations. It is able to load and edit a text in a content field or a file in the file system.
    /// Derived classes implement their custom logic (e.g. replace text) in a method override.
    /// </summary>
    public abstract class EditText : Step
    {
        public string Path { get; set; }
        public string Field { get; set; }
        public PathRelativeTo PathIsRelativeTo { get; set; } = PathRelativeTo.TargetDirectory;

        public override void Execute(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Path))
                throw new PackagingException(SR.Errors.InvalidParameters);

            var path = (string)context.ResolveVariable(Path);
            
            // if Path refers to a content
            if (RepositoryPath.IsValidPath(path) == RepositoryPath.PathResult.Correct)
            {
                Logger.LogMessage(path);

                ExecuteOnContent(context);
                return;
            }

            // edit text files in the file system
            foreach (var targetPath in ResolvePaths(path, context).Where(File.Exists))
            {
                Logger.LogMessage(targetPath);

                // we do not want to catch exceptions here: the step should fail in case of an error
                var text = File.ReadAllText(targetPath);

                text = Edit(text, context);

                // remove readonly flag from the file
                var fi = new FileInfo(targetPath);
                if (fi.IsReadOnly)
                    fi.IsReadOnly = false;

                File.WriteAllText(targetPath, text);
            }
        }
        private void ExecuteOnContent(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var path = (string)context.ResolveVariable(Path);
            var content = Content.Load(path);
            var data = content[Field ?? "Binary"];

            BinaryData binaryData = null;
            var text = data as string;
            if (text == null)
            {
                binaryData = data as BinaryData;
                if (binaryData != null)
                {
                    using (var r = new StreamReader(binaryData.GetStream()))
                        text = r.ReadToEnd();
                }
            }

            text = Edit(text, context);

            if (binaryData != null)
                binaryData.SetStream(RepositoryTools.GetStreamFromString(text));
            else
                content[Field] = text;

            content.SaveSameVersion();
        }

        /// <summary>
        /// Performs the text operation. Derived classes should implement their custom logic in this method.
        /// </summary>
        /// <param name="text">The loaded original text.</param>
        /// <param name="context">Context information about the current phase.</param>
        /// <returns>The manipulated text or null.</returns>
        protected abstract string Edit(string text, ExecutionContext context);

        protected IEnumerable<string> ResolvePaths(string path, ExecutionContext context)
        {
            return PathIsRelativeTo == PathRelativeTo.Package 
                ? new[] { ResolvePackagePath(path, context)} 
                : ResolveAllTargets(path, context);
        }
    }
}

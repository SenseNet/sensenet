using System;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;

namespace SenseNet.Packaging.Steps.Internal
{
    public class UndoChanges : Step
    {
        /// <summary>Content types to check for locked status and undo changes. Empty means all types.</summary>
        [DefaultProperty]
        [Annotation("Content types to check for locked status and undo changes. Empty means all types.")]
        public string ContentTypes { get; set; }
        /// <summary>Path of the subtree where changes should be reverted. Default is the root.</summary>
        [Annotation("Path of the subtree where changes should be reverted. Default is the root.")]
        public string Path { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var typeNames = context.ResolveVariable(ContentTypes) as string;
            if (string.IsNullOrEmpty(typeNames))
                typeNames = typeof(GenericContent).Name;

            var typeNameArray = typeNames.Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);
            var path = context.ResolveVariable(Path) as string;

            UndoContentChanges(path, typeNameArray);
        }

        internal static void UndoContentChanges(string path, params string[] typeNameArray)
        {
            if (typeNameArray == null || typeNameArray.Length == 0)
                typeNameArray = new[] {typeof(GenericContent).Name};
            if (string.IsNullOrEmpty(path))
                path = "/Root";

            var error = false;

            using (new SystemAccount())
            {
                Parallel.ForEach(ContentQuery
                        .Query(SafeQueries.LockedContentByPath, QuerySettings.AdminSettings, typeNameArray, path).Nodes
                        .Where(n => n is GenericContent).Cast<GenericContent>(),
                    new ParallelOptions {MaxDegreeOfParallelism = 10},
                    gc =>
                    {
                        Logger.LogMessage($"UNDO changes: {gc.Path}");

                        try
                        {
                            gc.UndoCheckOut();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            var msg = $"Error during undo changes of {gc.Path} (v: {gc.Version}): {ex.Message}";
                            SnLog.WriteException(ex, msg);
                            Logger.LogException(ex, msg);
                            System.Diagnostics.Trace.WriteLine(msg);
                        }
                    });
            }

            if (error)
                throw new InvalidOperationException("One or more errors occurred during UndoChanges, please check the log.");
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;

namespace SenseNet.Packaging.Steps.Internal
{
    public class UndoChanges : Step
    {
        /// <summary>Content types to check for locked status and undo changes. Empty means all types.</summary>
        [DefaultProperty]
        [Annotation("Content types to check for locked status and undo changes. Empty means all types.")]
        public string ContentTypes { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var typeNames = context.ResolveVariable(ContentTypes) as string;
            if (string.IsNullOrEmpty(typeNames))
                typeNames = typeof(GenericContent).Name;

            var typeNameArray = typeNames.Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);

            UndoContentChanges(typeNameArray);
        }

        internal static void UndoContentChanges(string[] typeNameArray = null)
        {
            if (typeNameArray == null || typeNameArray.Length == 0)
                typeNameArray = new[] {typeof(GenericContent).Name};

            using (new SystemAccount())
            {
                Parallel.ForEach(ContentQuery.Query(SafeQueries.LockedContent, QuerySettings.AdminSettings,
                        new object[] {typeNameArray}).Nodes.Where(n => n is GenericContent).Cast<GenericContent>(),
                    new ParallelOptions {MaxDegreeOfParallelism = 10},
                    gc =>
                    {
                        Logger.LogMessage($"UNDO changes: {gc.Path}");
                        gc.UndoCheckOut();
                    });
            }
        }
    }
}

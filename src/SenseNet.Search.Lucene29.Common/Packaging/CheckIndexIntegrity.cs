using System.Linq;
using SenseNet.Search.Lucene29;

namespace SenseNet.Packaging.Steps
{
    [Annotation("Checks the index integrity by comparation the index and database.")]
    public class CheckIndexIntegrity : Step
    {
        [DefaultProperty]
        [Annotation("Defines the integrity check's scope if there is. If empty, the whole repository tree will be checked.")]
        public string Path { get; set; }

        [Annotation("Defines whether check only one content or the whole tree or subtree. Default: true.")]
        public bool Recursive { get; set; } = true;

        [Annotation("Limits the output line count. 0 means all lines. Default: 1000.")]
        public int OutputLimit { get; set; } = 1000;

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var path = Path;
            if (!string.IsNullOrEmpty(path))
                path = path.Trim();

            Logger.LogMessage((Recursive ? "Recursive integrity check. Scope: " : "Integrity check on: ") + (path ?? "/Root"));

            var diff = IntegrityChecker.Check(path, Recursive).ToArray();
            Logger.LogMessage("Integrity check finished. Count of differences: " + diff.Length);

            var outputLimit = OutputLimit == 0 ? int.MaxValue : OutputLimit;
            var lines = 0;
            foreach (var d in diff)
            {
                Logger.LogMessage("  " + d);
                if (++lines >= outputLimit)
                {
                    Logger.LogMessage("...truncated...");
                    break;
                }
            }
        }
    }
}

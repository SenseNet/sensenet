using SenseNet.Search.Indexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Lucene29;

namespace SenseNet.Packaging.Steps
{
    [Annotation("Checks the index integrity by comparation the index and database.")]
    public class CheckIndexIntegrity : Step
    {
        private bool _recursive = true;
        private int _outputLimit = 1000;

        [DefaultProperty]
        [Annotation("Defines the integrity check's scope if there is. If empty, the whole repository tree will be checked.")]
        public string Path { get; set; }

        [Annotation("Defines whether check only one content or the whole tree or subtree. Default: true.")]
        public bool Recursive
        {
            get { return _recursive; }
            set { _recursive = value; }
        }

        [Annotation("Limits the output line count. 0 means all lines. Default: 1000.")]
        public int OutputLimit
        {
            get { return _outputLimit; }
            set { _outputLimit = value; }
        }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var path = Path;
            if (!string.IsNullOrEmpty(path))
                path = path.Trim();

            Logger.LogMessage((Recursive ? "Recursive integrity check. Scope: " : "Integrity check on: ") + (path ?? "/Root"));

            var diff = IntegrityChecker.Check(path, Recursive);
            Logger.LogMessage("Integrity check finished. Count of differences: " + diff.Count());

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

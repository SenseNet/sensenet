using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging.Steps
{
    public class ForEach : Step
    {
        public IEnumerable<XmlElement> Block { get; set; }

        public string Item { get; set;}
        public string Files { get; set; }
        public string ContentQuery { get; set; }

        protected virtual IEnumerable<object> Collection { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var collection = ResolveCollection(context);
            var i = 0;
            foreach (var item in collection)
            {
                Logger.LogMessage("{0} {1}. iteration", this.Item, ++i);
                SetVariable(Item, item, context);
                PackageManager.ExecuteSteps(Block?.ToList() ?? new List<XmlElement>(), context);
            }
        }

        protected virtual IEnumerable<object> ResolveCollection(ExecutionContext context)
        {
            if (this.Files != null)
                return ResolveFiles(context);
            if (this.ContentQuery != null)
                return ResolveContents(context);
            return new object[0];
        }

        private IEnumerable<string> ResolveFiles(ExecutionContext context)
        {
            var result = new List<string>();

            var path = this.Files;
            var isRooted = Path.IsPathRooted(path);

            if (path.Contains('?') || path.Contains('*'))
            {
                string dir;
                string pattern;
                if (path.Contains('\\'))
                {
                    dir = ResolveTargetPath(Path.GetDirectoryName(path), context);
                    pattern = Path.GetFileName(path);
                }
                else
                {
                    dir = context.TargetPath;
                    pattern = path;
                }
                var files = Directory.GetFiles(dir, pattern);
                result.AddRange(isRooted ? files : files.Select(f => f.Substring(context.TargetPath.Length + 1)));
            }
            else
            {
                result.Add(path);
            }

            return result;
        }

        private IEnumerable<Content> ResolveContents(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            var result = Search.ContentQuery.Query(ContentQuery);
            Logger.LogMessage("Content query result count: ", result.Count);
            return result.Nodes.Select(Content.Create);
        }
    }
}

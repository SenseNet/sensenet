using System;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Caching.Legacy
{
    internal class PathDependencyImpl : System.Web.Caching.CacheDependency
    {
        private readonly string _path;

        public PathDependencyImpl(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            _path = path.ToLowerInvariant();
            try
            {
                PathDependency.Subscribe(PathDependency_SubtreeChanged);
            }
            finally
            {
                FinishInit();
            }
        }
        protected override void DependencyDispose()
        {
            PathDependency.Unsubscribe(PathDependency_SubtreeChanged);
        }

        private void PathDependency_SubtreeChanged(object sender, EventArgs<string> e)
        {
            string path = e.Data.ToLowerInvariant();

            // Path matches?
            var match = _path == path;

            // If does not match, path starts with?
            if (!match)
                match = _path.StartsWith(string.Concat(e.Data, RepositoryPath.PathSeparator), StringComparison.OrdinalIgnoreCase);

            if (match)
            {
                NotifyDependencyChanged(this, e);
                SnTrace.Repository.Write("Cache invalidated by path: "+ _path);
            }
        }

    }
}

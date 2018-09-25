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
            if (PathDependency.IsChanged(e.Data, _path))
                NotifyDependencyChanged(this, e);
        }

    }
}

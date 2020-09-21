using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging
{
    internal static class Extensions
    {
        public static Version ToVersion(this string src)
        {
            return Version.Parse(src.TrimStart('v', 'V'));
        }
    }
    public class PatchBuilder //UNDONE:PATCH: Need to be tested.
    {
        private readonly ISnComponent _component;
        internal List<ISnPatch> Patches { get; } = new List<ISnPatch>();

        public PatchBuilder(ISnComponent component)
        {
            _component = component;
        }

        public VersionBoundary Version(string version)
        {
            return new VersionBoundary{MinVersion = version.ToVersion(), MaxVersion = version.ToVersion()};
        }
        public VersionBoundary MinVersion(string version)
        {
            return new VersionBoundary { MinVersion = version.ToVersion() };
        }
        public VersionBoundary MinMaxExVersion(string min, string maxEx)
        {
            return new VersionBoundary
            {
                MinVersion = min.ToVersion(),
                MaxVersion = maxEx.ToVersion(),
                MaxVersionIsExclusive = true
            };
        }

        public DependencyBuilder Dependency(string componentId, string minVersion)
        {
            var builder = new DependencyBuilder(this);
            builder.Dependency(componentId, this.MinVersion(minVersion));
            return builder;
        }
        public DependencyBuilder Dependency(string componentId, string minVersion, string maxExVersion)
        {
            var builder = new DependencyBuilder(this);
            builder.Dependency(componentId, this.MinMaxExVersion(minVersion, maxExVersion));
            return builder;
        }
        public DependencyBuilder Dependency(string componentId, VersionBoundary boundary)
        {
            var builder = new DependencyBuilder(this);
            builder.Dependency(componentId, boundary);
            return builder;
        }

        public PatchBuilder Patch(string version, string released, string description, VersionBoundary boundary,
            DependencyBuilder dependencies,
            Action<PatchExecutionContext> execute)
        {
            Patches.Add(new SnPatch
            {
                ComponentId = _component.ComponentId,
                ReleaseDate = DateTime.Parse(released),
                Boundary = boundary,
                Dependencies = dependencies?.Dependencies.ToArray(),
                Version = System.Version.Parse(version.TrimStart('v', 'V')),
                Description = description,
                Execute = execute
            });

            return this;
        }
        public PatchBuilder Install(string version, string released, string description,
            DependencyBuilder dependencies,
            Action<PatchExecutionContext> execute)
        {
            Patches.Add(new ComponentInstaller
            {
                ComponentId = _component.ComponentId,
                ReleaseDate = DateTime.Parse(released),
                Dependencies = dependencies?.Dependencies.ToArray(),
                Version = System.Version.Parse(version.TrimStart('v', 'V')),
                Description = description,
                Execute = execute
            });

            return this;
        }
    }
    public class DependencyBuilder
    {
        private readonly PatchBuilder _patchBuilder;
        public List<Dependency> Dependencies { get; } = new List<Dependency>();

        internal DependencyBuilder(PatchBuilder patchBuilder)
        {
            _patchBuilder = patchBuilder;
        }

        public DependencyBuilder Dependency(string componentId, string minVersion)
        {
            Dependencies.Add(new Dependency { Id = componentId, Boundary = _patchBuilder.MinVersion(minVersion) });
            return this;
        }
        public DependencyBuilder Dependency(string componentId, string minVersion, string maxExVersion)
        {
            Dependencies.Add(new Dependency
            {
                Id = componentId,
                Boundary = _patchBuilder.MinMaxExVersion(minVersion, maxExVersion)
            });
            return this;
        }
        public DependencyBuilder Dependency(string componentId, VersionBoundary boundary)
        {
            Dependencies.Add(new Dependency { Id = componentId, Boundary = boundary });
            return this;
        }
    }
}

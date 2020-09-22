using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PatchBuilder
    {
        private readonly ISnComponent _component;
        private readonly List<ISnPatch> _patches = new List<ISnPatch>();

        internal List<ISnPatch> GetPatches()
        {
            //TODO:PATCH: Execute other validation here if needed.
            return _patches;
        }

        public PatchBuilder(ISnComponent component)
        {
            _component = component;
        }

        internal VersionBoundary MinVersion(string version)
        {
            return new VersionBoundary { MinVersion = version.ToVersion() };
        }
        internal VersionBoundary MinMaxExVersion(string min, string maxEx)
        {
            return new VersionBoundary
            {
                MinVersion = min.ToVersion(),
                MaxVersion = maxEx.ToVersion(),
                MaxVersionIsExclusive = true
            };
        }

        public ItemBuilder Install(string version, string released, string description)
        {
            var patch = new ComponentInstaller
            {
                ComponentId = _component.ComponentId,
                ReleaseDate = ParseDate(released),
                Version = ParseVersion(version),
                Description = CheckDescription(description),
            };
            _patches.Add(patch);

            return new ItemBuilder(patch, this);
        }

        public ItemBuilder Patch(string from, string to, string released, string description)
        {
            return Patch(ParseFromVersion(from), to, released, description);
        }
        public ItemBuilder Patch(VersionBoundary from, string to, string released, string description)
        {
            var targetVersion = ParseVersion(to);

            var patch = new SnPatch
            {
                ComponentId = _component.ComponentId,
                ReleaseDate = ParseDate(released),
                Boundary = BuildBoundary(from, targetVersion),
                Version = targetVersion,
                Description = CheckDescription(description),
            };
            _patches.Add(patch);

            return new ItemBuilder(patch, this);
        }

        private VersionBoundary BuildBoundary(VersionBoundary boundary, Version targetVersion)
        {
            if (boundary.MinVersion != null)
            {
                if (targetVersion <= boundary.MinVersion)
                    throw new InvalidPatchException(PatchErrorCode.TooSmallTargetVersion,
                        "The 'Version' need to be higher than minimal version.");
            }

            if (boundary.MaxVersion == null)
            {
                boundary.MaxVersion = targetVersion;
                boundary.MaxVersionIsExclusive = true;
            }

            return boundary;
        }

        private VersionBoundary ParseFromVersion(string src)
        {
            return new VersionBoundary { MinVersion = ParseVersion(src) };
        }
        internal static Version ParseVersion(string version)
        {
            try
            {
                return Version.Parse(version.TrimStart('v', 'V'));
            }
            catch (Exception e)
            {
                throw new InvalidPatchException(PatchErrorCode.InvalidVersion, e.Message, e);
            }
        }
        private DateTime ParseDate(string date)
        {
            try
            {
                return DateTime.Parse(date);
            }
            catch (Exception e)
            {
                throw new InvalidPatchException(PatchErrorCode.InvalidDate, e.Message, e);
            }
        }
        private string CheckDescription(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new InvalidPatchException(PatchErrorCode.MissingDescription,
                    "The description cannot be null or empty.");
            return text;
        }
    }

    public class ItemBuilder
    {
        private readonly ISnPatch _patch;
        private readonly PatchBuilder _patchBuilder;
        internal ItemBuilder(ISnPatch patch, PatchBuilder patchBuilder)
        {
            _patch = patch;
            _patchBuilder = patchBuilder;
        }

        public ItemBuilder DependsFrom(string componentId, VersionBoundary boundary)
        {
            AddDependency(new Dependency { Id = componentId, Boundary = boundary });
            return this;
        }
        public ItemBuilder DependsFrom(string componentId, string minVersion)
        {
            AddDependency(new Dependency
            {
                Id = componentId,
                Boundary = new VersionBoundary
                {
                    MinVersion = PatchBuilder.ParseVersion(minVersion)
                }
            });
            return this;
        }
        public ItemBuilder DependsFrom(DependencyBuilder builder)
        {
            var deps = _patch.Dependencies?.ToList() ?? new List<Dependency>();
            deps.AddRange(builder.Dependencies);
            _patch.Dependencies = deps;
            return this;
        }
        private void AddDependency(Dependency dependency)
        {
            var deps = _patch.Dependencies?.ToList() ?? new List<Dependency>();
            deps.Add(dependency);
            _patch.Dependencies = deps;
        }

        public PatchBuilder Execute(Action<PatchExecutionContext> executeAction = null)
        {
            if (executeAction != null)
                _patch.Execute = executeAction;
            return _patchBuilder;
        }
    }
    public class DependencyBuilder
    {
        private readonly PatchBuilder _patchBuilder;
        public List<Dependency> Dependencies { get; } = new List<Dependency>();

        public DependencyBuilder(PatchBuilder patchBuilder)
        {
            _patchBuilder = patchBuilder;
        }

        public DependencyBuilder Dependency(string componentId, string minVersion)
        {
            Dependencies.Add(new Dependency { Id = componentId, Boundary = _patchBuilder.MinVersion(minVersion) });
            return this;
        }
        public DependencyBuilder Dependency(string componentId, VersionBoundary boundary)
        {
            Dependencies.Add(new Dependency { Id = componentId, Boundary = boundary });
            return this;
        }
    }
}

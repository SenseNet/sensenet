﻿using System;
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
        public PatchBuilderAfterPatch Install(string version, string released, string description)
        {
            var patch = new ComponentInstaller
            {
                ComponentId = _component.ComponentId,
                Version = new Version(0, 0)
            };

            patch.Version = ParseVersion(version, patch);
            patch.ReleaseDate = ParseDate(released, patch);
            patch.Description = CheckDescription(description, patch);
            _patches.Add(patch);

            return new PatchBuilderAfterPatch(patch, this);
        }
        public PatchBuilderAfterPatch Patch(string from, string to, string released, string description)
        {
            var patch = BuildSnPatch(null, from, to, released, description);
            return new PatchBuilderAfterPatch(patch, this);
        }
        public PatchBuilderAfterPatch Patch(VersionBoundary from, string to, string released, string description)
        {
            var patch = BuildSnPatch(from, null, to, released, description);
            return new PatchBuilderAfterPatch(patch, this);
        }
        internal SnPatch BuildSnPatch(VersionBoundary from, string fromSrc, string to, string released, string description)
        {
            var patch = new SnPatch
            {
                ComponentId = _component.ComponentId,
                Version = new Version(0, 0),
                Boundary = new VersionBoundary
                {
                    MinVersion = new Version(0, 0),
                    MaxVersion = new Version(0, 0)
                }
            };

            var targetVersion = ParseVersion(to, patch);
            patch.Version = targetVersion;
            patch.Boundary = BuildBoundary(from ?? ParseFromVersion(fromSrc, patch), targetVersion);
            patch.ReleaseDate = ParseDate(released, patch);
            patch.Description = CheckDescription(description, patch);

            if (patch.Boundary.MinVersion != null)
            {
                if (targetVersion <= patch.Boundary.MinVersion)
                    throw new InvalidPatchException(PatchErrorCode.TooSmallTargetVersion, patch,
                        "The 'Version' need to be higher than minimal version.");
            }

            _patches.Add(patch);

            return patch;
        }

        /* =========================================================================================== INTERNAL PART */

        private readonly ISnComponent _component;
        private readonly List<ISnPatch> _patches = new List<ISnPatch>();

        internal PatchBuilder(ISnComponent component)
        {
            _component = component;
        }

        internal List<ISnPatch> GetPatches()
        {
            //TODO:PATCH: Execute other validation here if needed.
            return _patches;
        }

        private VersionBoundary BuildBoundary(VersionBoundary boundary, Version targetVersion)
        {
            if (boundary.MaxVersion == null)
            {
                boundary.MaxVersion = targetVersion;
                boundary.MaxVersionIsExclusive = true;
            }

            return boundary;
        }
        private VersionBoundary ParseFromVersion(string src, ISnPatch patch)
        {
            return new VersionBoundary { MinVersion = ParseVersion(src, patch) };
        }

        internal static Version ParseVersion(string version, ISnPatch patch)
        {
            try
            {
                return Version.Parse(version.TrimStart('v', 'V'));
            }
            catch (Exception e)
            {
                throw new InvalidPatchException(PatchErrorCode.InvalidVersion, patch, e.Message, e);
            }
        }
        private DateTime ParseDate(string date, ISnPatch patch)
        {
            try
            {
                return DateTime.Parse(date);
            }
            catch (Exception e)
            {
                throw new InvalidPatchException(PatchErrorCode.InvalidDate, patch, e.Message, e);
            }
        }
        private string CheckDescription(string text, ISnPatch patch)
        {
            if (string.IsNullOrEmpty(text))
                throw new InvalidPatchException(PatchErrorCode.MissingDescription, patch,
                    "The description cannot be null or empty.");
            return text;
        }

    }
    public class PatchBuilderAfterPatch
    {
        private readonly SnPatchBase _patch;
        private readonly PatchBuilder _patchBuilder;

        internal PatchBuilderAfterPatch(SnPatchBase patch, PatchBuilder patchBuilder)
        {
            _patch = patch;
            _patchBuilder = patchBuilder;
        }

        public PatchBuilderAfterPatch DependsOn(string componentId, string minVersion)
        {
            return DependsOn(componentId, new VersionBoundary
            {
                MinVersion = PatchBuilder.ParseVersion(minVersion, _patch)
            });
        }
        public PatchBuilderAfterPatch DependsOn(string componentId, VersionBoundary boundary)
        {
            AddDependency(new Dependency { Id = componentId, Boundary = boundary });
            return this;
        }
        public PatchBuilderAfterPatch DependsOn(DependencyBuilder builder)
        {
            var deps = _patch.Dependencies?.ToList() ?? new List<Dependency>();
            foreach (var dep in builder.Dependencies)
            {
                AssertDependencyIsValid(dep, deps);
                deps.Add(dep);
            }
            _patch.Dependencies = deps;
            return this;
        }
        private void AddDependency(Dependency dependency)
        {
            var deps = _patch.Dependencies?.ToList() ?? new List<Dependency>();
            AssertDependencyIsValid(dependency, deps);
            deps.Add(dependency);
            _patch.Dependencies = deps;
        }
        private void AssertDependencyIsValid(Dependency dependencyToAdd, List<Dependency> dependencies)
        {
            if (dependencyToAdd.Id == this._patch.ComponentId)
                throw new InvalidPatchException(PatchErrorCode.SelfDependency, _patch,
                    "Self dependency is forbidden: " + _patch);
            if (dependencies.Any(d => d.Id == dependencyToAdd.Id))
                throw new InvalidPatchException(PatchErrorCode.DuplicatedDependency, _patch,
                    "Duplicated dependency is forbidden: " + _patch);
        }

        public PatchBuilderAfterActionOnBefore ActionOnBefore(Action<PatchExecutionContext> action = null)
        {
            if (action != null)
                _patch.ActionBeforeStart = action;
            return new PatchBuilderAfterActionOnBefore(_patch, _patchBuilder);
        }
        public PatchBuilderAfterAction Action(Action<PatchExecutionContext> action = null)
        {
            if (action != null)
                _patch.Action = action;
            return new PatchBuilderAfterAction(_patch, _patchBuilder);
        }

        public PatchBuilderAfterPatch Patch(string from, string to, string released, string description)
        {
            var patch = _patchBuilder.BuildSnPatch(null, from, to, released, description);
            return new PatchBuilderAfterPatch(patch, _patchBuilder);
        }
        public PatchBuilderAfterPatch Patch(VersionBoundary from, string to, string released, string description)
        {
            var patch = _patchBuilder.BuildSnPatch(from, null, to, released, description);
            return new PatchBuilderAfterPatch(patch, _patchBuilder);
        }
    }
    public class PatchBuilderAfterActionOnBefore
    {
        private readonly SnPatchBase _patch;
        private readonly PatchBuilder _patchBuilder;

        internal PatchBuilderAfterActionOnBefore(SnPatchBase patch, PatchBuilder patchBuilder)
        {
            _patch = patch;
            _patchBuilder = patchBuilder;
        }

        public PatchBuilderAfterAction Action(Action<PatchExecutionContext> action = null)
        {
            if (action != null)
                _patch.Action = action;
            return new PatchBuilderAfterAction(_patch, _patchBuilder);
        }

        public PatchBuilderAfterPatch Patch(string from, string to, string released, string description)
        {
            var patch = _patchBuilder.BuildSnPatch(null, from, to, released, description);
            return new PatchBuilderAfterPatch(patch, _patchBuilder);
        }
        public PatchBuilderAfterPatch Patch(VersionBoundary from, string to, string released, string description)
        {
            var patch = _patchBuilder.BuildSnPatch(from, null, to, released, description);
            return new PatchBuilderAfterPatch(patch, _patchBuilder);
        }
    }
    public class PatchBuilderAfterAction
    {
        private readonly SnPatchBase _patch;
        private readonly PatchBuilder _patchBuilder;

        internal PatchBuilderAfterAction(SnPatchBase patch, PatchBuilder patchBuilder)
        {
            _patch = patch;
            _patchBuilder = patchBuilder;
        }

        public PatchBuilderAfterPatch Patch(string from, string to, string released, string description)
        {
            var patch = _patchBuilder.BuildSnPatch(null, from, to, released, description);
            return new PatchBuilderAfterPatch(patch, _patchBuilder);
        }
        public PatchBuilderAfterPatch Patch(VersionBoundary from, string to, string released, string description)
        {
            var patch = _patchBuilder.BuildSnPatch(from, null, to, released, description);
            return new PatchBuilderAfterPatch(patch, _patchBuilder);
        }
    }
    public class DependencyBuilder
    {
        public List<Dependency> Dependencies { get; } = new List<Dependency>();

        public DependencyBuilder Dependency(string componentId, string minVersion)
        {
            Dependencies.Add(new Dependency { Id = componentId, Boundary = MinVersion(minVersion) });
            return this;
        }
        public DependencyBuilder Dependency(string componentId, VersionBoundary boundary)
        {
            Dependencies.Add(new Dependency { Id = componentId, Boundary = boundary });
            return this;
        }

        internal VersionBoundary MinVersion(string version)
        {
            return new VersionBoundary { MinVersion = version.ToVersion() };
        }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Packaging.Tests
{
    [TestClass]
    public class PatchingTestBase : PackagingTestBase
    {
        //[TestMethod]
        //public async Task Patching_Collect_NoDependency()
        //{
        //    // ARRANGE
        //    await SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C1", "1.1", "02:00", "2016-01-06", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C1", "1.2", "03:00", "2016-01-09", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C2", "1.0", "01:10", "2016-01-01", PackageType.Install, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C2", "1.1", "02:10", "2016-01-06", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C2", "1.2", "03:10", "2016-01-09", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);

        //    var patches = new[]
        //    {
        //        Patch("C1", "       v = 1.0", "1.1", null),
        //        Patch("C3", "       v < 1.1", "1.1", null),
        //        Patch("C2", "1.2 <= v < 1.3", "1.3", null),
        //        Patch("C1", "1.2 <= v < 1.3", "1.3", null),
        //        Patch("C1", "1.1 <= v < 1.2", "1.2", null),
        //    };

        //    // ACTION
        //    var ordered = await GetOrderedPatches(patches, CancellationToken.None).ConfigureAwait(false);

        //    // ASSERT
        //    var actual = ordered.OrderBy(x => x.Id).ToArray();
        //    Assert.AreEqual(2, ordered.Length);
        //    Assert.AreEqual("C1", actual[0].Id);
        //    Assert.AreEqual("C2", actual[1].Id);
        //}
        //[TestMethod]
        //public async Task Patching_Collect_OneDependency()
        //{
        //    await SavePackage("C1", "1.0", "01:00", "2016-01-01", PackageType.Install, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C1", "1.1", "02:00", "2016-01-06", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C1", "1.2", "03:00", "2016-01-09", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C2", "1.0", "01:10", "2016-01-01", PackageType.Install, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C2", "1.1", "02:10", "2016-01-06", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);
        //    await SavePackage("C2", "1.2", "03:10", "2016-01-09", PackageType.Patch, ExecutionResult.Successful).ConfigureAwait(false);

        //    var patches = new[]
        //    {
        //        Patch("C1", "       v = 1.0", "1.1", null),
        //        Patch("C3", "       v < 1.1", "1.1", null),
        //        Patch("C2", "1.2 <= v < 1.3", "1.3", new[] {Dep("C1", "=1.3")}),
        //        Patch("C1", "1.2 <= v < 1.3", "1.3", null),
        //        Patch("C1", "1.1 <= v < 1.2", "1.2", null),
        //    };

        //    var ordered = await GetOrderedPatches(patches, CancellationToken.None).ConfigureAwait(false);

        //    Assert.AreEqual(2, ordered.Length);
        //    Assert.AreEqual("C1", ordered[0].Id);
        //    Assert.AreEqual("C2", ordered[1].Id);
        //}

        /* ================================================================= Steps of Packaging logic algorithms */

        protected void ValidatePatch(SnPatch patch)
        {
            if (patch.ComponentId == null)
                throw new InvalidPackageException("The Id cannot be null.",
                    PackagingExceptionType.MissingComponentId);
            if (patch.ComponentId.Length == 0)
                throw new InvalidPackageException("The Id cannot be empty.",
                    PackagingExceptionType.MissingComponentId);
            if (patch.Version == null)
                throw new InvalidPackageException("Missing Version.",
                    PackagingExceptionType.MissingVersion);

            // If MaxVersion is not defined, it need to be less than the target version
            var boundary = patch.Boundary;
            var maxVer = boundary.MaxVersion;
            if (maxVer.Major == int.MaxValue && maxVer.Minor == int.MaxValue)
            {
                boundary.MaxVersion = patch.Version;
                boundary.MaxVersionIsExclusive = true;
            }

            if (boundary.MaxVersionIsExclusive && patch.Version < boundary.MaxVersion)
                throw new InvalidPackageException("Version too small.",
                    PackagingExceptionType.TargetVersionTooSmall);
            if (!boundary.MaxVersionIsExclusive && patch.Version <= boundary.MaxVersion)
                throw new InvalidPackageException("Version too small.",
                    PackagingExceptionType.TargetVersionTooSmall);

            if (boundary.MaxVersion < boundary.MinVersion)
                throw new InvalidPackageException("Maximum version is less than minimum version.",
                    PackagingExceptionType.MaxLessThanMin);

            if (boundary.MinVersion == boundary.MaxVersion &&
                boundary.MinVersionIsExclusive && boundary.MaxVersionIsExclusive)
                throw new InvalidPackageException("Maximum and minimum versions are equal but both versions are exclusive.",
                    PackagingExceptionType.InvalidInterval);
            if (boundary.MinVersion == boundary.MaxVersion && boundary.MinVersionIsExclusive)
                throw new InvalidPackageException("Maximum and minimum versions are equal but the minimum version is exclusive.",
                    PackagingExceptionType.InvalidInterval);
            if (boundary.MinVersion == boundary.MaxVersion && boundary.MaxVersionIsExclusive)
                throw new InvalidPackageException("Maximum and minimum versions are equal but the maximum version is exclusive.",
                    PackagingExceptionType.InvalidInterval);

            if (patch.Dependencies != null)
            {
                if (patch.Dependencies.Any(x => x.Id == patch.ComponentId))
                    throw new InvalidPackageException("Patch and dependency id are the same.",
                        PackagingExceptionType.PatchIdAndDependencyIdAreTheSame);
            }
        }
        protected void ValidatePatches(SnPatch patch1, SnPatch patch2)
        {
            if (patch1.Version == patch2.Version)
                throw new InvalidPackageException(
                    $"Target versions are the same. ComponentId: {patch1.ComponentId}, version: {patch1.Version}",
                    PackagingExceptionType.TargetVersionsAreTheSame);

            if (patch1.Boundary.MinVersion == patch1.Boundary.MaxVersion &&
                patch2.Boundary.MinVersion == patch2.Boundary.MaxVersion &&
                patch1.Boundary.MinVersion == patch2.Boundary.MinVersion)
                throw new InvalidPackageException(
                    $"Target versions are the same. ComponentId: {patch1.ComponentId}, version: {patch1.Boundary.MinVersion}",
                    PackagingExceptionType.SourceVersionsAreTheSame);

            // Ordering: the patch2 need to be higher
            if (patch1.Version > patch2.Version)
            {
                var temp = patch1;
                patch1 = patch2;
                patch2 = temp;
            }

            // (C1: 1.0 <= v <  2.0, v2.0) (C1: 1.9 <= v <  3.0, v3.0) Overlapped
            if (patch1.Boundary.MaxVersion > patch2.Boundary.MinVersion)
                throw new InvalidPackageException(
                    $"Overlapped intervals. Id: {patch1.ComponentId}, versions: {patch1.Version}, {patch2.Version}",
                    PackagingExceptionType.OverlappedIntervals);
            // (C1: v <= 2.0, v2.0) (C1: 2.0 <= v, v3.0) Overlapped
            if (patch1.Boundary.MaxVersion == patch2.Boundary.MinVersion &&
                !patch1.Boundary.MaxVersionIsExclusive && !patch2.Boundary.MinVersionIsExclusive)
                throw new InvalidPackageException(
                    $"Overlapped intervals. Id: {patch1.ComponentId}, versions: {patch1.Version}, {patch2.Version}",
                    PackagingExceptionType.OverlappedIntervals);
        }

        protected SnPatch[] GetRelevantPatches(IEnumerable<Package> installedPackages, IEnumerable<SnPatch> patches)
        {
            var lastVersions = installedPackages.GroupBy(x => x.ComponentId)
                .Select(y => new { ComponentId = y.Key, Version = y.Max(row => row.ComponentVersion) })
                .ToArray();

            var relevantPatches = patches.Where(patch =>
                lastVersions.Any(pkg => pkg.ComponentId == patch.ComponentId &&
                                        patch.Version > pkg.Version &&
                                        patch.Boundary.IsInInterval(pkg.Version)));

            return relevantPatches.ToArray();
        }

        /* ========================================================================================== */

        /// <summary>
        /// Creates a successfully executed package for test purposes. PackageType = PackageType.Patch.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Version after successful execution</param>
        /// <param name="dependencies">Dependency array. Use null if there is no dependencies.</param>
        /// <returns></returns>
        protected Package Package(string id, string version, Dependency[] dependencies)
        {
            var package = new Package
            {
                ComponentId = id,
                ComponentVersion = Version.Parse(version.TrimStart('v')),
                Description = $"{id}-Description",
                ExecutionDate = DateTime.Now.AddDays(-1),
                ReleaseDate = DateTime.Now.AddDays(-2),
                ExecutionError = null,
                ExecutionResult = ExecutionResult.Successful,
                PackageType = PackageType.Patch,
            };

            package.Manifest = Manifest.Create(package, dependencies, false).ToXmlString();

            return package;
        }

        /// <summary>
        /// Creates a SnComponentDescriptor for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Last saved version.</param>
        /// <returns></returns>
        protected SnComponentDescriptor Comp(string id, string version)
        {
            return new SnComponentDescriptor(id, Version.Parse(version.TrimStart('v')), "", null);
        }

        protected ComponentInstaller Inst(string id, string version)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = null,
            };
        }
        protected ComponentInstaller Inst(string id, string version, Dependency[] dependencies)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = dependencies,
            };
        }
        protected ComponentInstaller Inst(string id, string version,
            Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = null,
                Action = action
            };
        }
        protected ComponentInstaller Inst(string id, string version, Dependency[] dependencies,
            Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = dependencies,
                Action = action
            };
        }
        protected ComponentInstaller Inst(string id, string version, Action<PatchExecutionContext> actionBefore,
            Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = null,
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }
        protected ComponentInstaller Inst(string id, string version, Dependency[] dependencies,
            Action<PatchExecutionContext> actionBefore, Action<PatchExecutionContext> action)
        {
            return new ComponentInstaller
            {
                ComponentId = id,
                Version = Version.Parse(version.TrimStart('v')),
                Dependencies = dependencies,
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }

        /// <summary>
        /// Creates a patch for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Target version</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <returns></returns>
        protected SnPatch Patch(string id, string boundary, string version)
        {
            return Patch(id, boundary, version, null, null);
        }
        /// <summary>
        /// Creates a patch for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Target version</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <param name="dependencies">Dependency array. Use null if there is no dependencies.</param>
        /// <returns></returns>
        protected SnPatch Patch(string id, string boundary, string version, Dependency[] dependencies)
        {
            return new SnPatch
            {
                ComponentId = id,
                Version = version == null ? null : Version.Parse(version.TrimStart('v')),
                Boundary = ParseBoundary(boundary),
                Dependencies = dependencies
            };
        }
        /// <summary>
        /// Creates a patch for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Target version</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <param name="action">Function of execution</param>
        /// <returns></returns>
        protected SnPatch Patch(string id, string boundary, string version, Action<PatchExecutionContext> action)
        {
            return Patch(id, boundary, version, null, action);
        }
        protected SnPatch Patch(string id, string boundary, string version, Dependency[] dependencies, 
            Action<PatchExecutionContext> actionBefore, Action<PatchExecutionContext> action)
        {
            return new SnPatch
            {
                ComponentId = id,
                Version = version == null ? null : Version.Parse(version.TrimStart('v')),
                Boundary = ParseBoundary(boundary),
                Dependencies = dependencies,
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }
        protected SnPatch Patch(string id, string boundary, string version, Action<PatchExecutionContext> actionBefore,
            Action<PatchExecutionContext> action)
        {
            return new SnPatch
            {
                ComponentId = id,
                Version = version == null ? null : Version.Parse(version.TrimStart('v')),
                Boundary = ParseBoundary(boundary),
                ActionBeforeStart = actionBefore,
                Action = action
            };
        }

        /// <summary>
        /// Creates a Dependency for test purposes.
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="boundary">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <returns></returns>
        protected Dependency Dep(string id, string boundary)
        {
            return new Dependency
            {
                Id = id,
                Boundary = ParseBoundary(boundary)
            };
        }

        protected VersionBoundary ParseBoundary(string src)
        {
            // "1.0 <= v <  2.0"

            var a = src.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            var boundary = new VersionBoundary();

            if (a.Length == 3)
            {
                if (a[0] == "v")
                {
                    boundary.MaxVersion = Version.Parse(a[2]);
                    boundary.MaxVersionIsExclusive = a[1] == "<";

                    boundary.MinVersion = Version.Parse("0.0");
                    boundary.MinVersionIsExclusive = false;
                }
                else if (a[2] == "v")
                {
                    boundary.MinVersion = Version.Parse(a[0]);
                    boundary.MinVersionIsExclusive = a[1] == "<";

                    boundary.MaxVersion = new Version(int.MaxValue, int.MaxValue);
                    boundary.MaxVersionIsExclusive = false;
                }
                else
                {
                    throw new FormatException($"Invalid Boundary: {src}");
                }
            }
            else if (a.Length == 5 && a[2] == "v")
            {
                boundary.MinVersion = Version.Parse(a[0]);
                boundary.MinVersionIsExclusive = a[1] == "<";

                boundary.MaxVersion = Version.Parse(a[4]);
                boundary.MaxVersionIsExclusive = a[3] == "<";
            }
            else
            {
                throw new FormatException($"Invalid Boundary: {src}");
            }

            return boundary;
        }


        protected string ComponentsToString(SnComponentDescriptor[] components)
        {
            return string.Join(" ", components.OrderBy(x => x.ComponentId)
                .Select(x => $"{x.ComponentId}v{x.Version}"));
        }
        protected string ComponentsToStringWithResult(SnComponentDescriptor[] components)
        {
            return string.Join(" ", components.OrderBy(x => x.ComponentId)
                .Select(x => $"{x.ComponentId}v{x.Version}({x.FaultyBeforeVersion},{x.FaultyAfterVersion})"));
        }
        protected string PatchesToString(ISnPatch[] executables)
        {
            return string.Join(" ", executables.Select(x =>
                $"{x.ComponentId}{(x.Type == PackageType.Install ? "i" : "p")}{x.Version}"));
        }
        protected string PackagesToString(Package[] packages)
        {
            return string.Join("|", packages.Select(p => p.ToString()));
        }

    }
}

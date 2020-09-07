using System;
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

        private async Task<SnPatch[]> GetOrderedPatches(SnPatch[] patches, CancellationToken cancellationToken)
        {
            var packages = (await PackageManager.Storage.LoadInstalledPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false));

            var lastVersions = packages.GroupBy(x => x.ComponentId)
                .Select(y => new {Id = y.Key, Version = y.Max(row => row.ComponentVersion)})
                .ToArray();

            var relevantPatches = patches.Where(patch =>
                lastVersions.Any(pkg => pkg.Id == patch.Id && patch.Version > pkg.Version));

            //UNDONE: dependency ?!

            return relevantPatches.ToArray();
        }
        private bool IsVersionRelevant(SnPatch patch, Version packageVersion)
        {
            if (patch.Version < packageVersion)
                return false;

            if (patch.Boundary.MinVersionIsExclusive)
            {
                if (patch.Boundary.MinVersion >= packageVersion)
                    return false;
            }
            else
            {
                if (patch.Boundary.MinVersion > packageVersion)
                    return false;
            }

            if (patch.Boundary.MaxVersion == null)
                return true;

            if (patch.Boundary.MaxVersionIsExclusive)
            {
                if (patch.Boundary.MaxVersion <= packageVersion)
                    return false;
            }
            else
            {
                if (patch.Boundary.MaxVersion < packageVersion)
                    return false;
            }

            return true;
        }

        /* ========================================================================================== */

        /// <summary>
        /// Creates a patch
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="version">Target version</param>
        /// <param name="fromTo">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <param name="dependencies">Dependency array. Use null if there is no dependencies.</param>
        /// <returns></returns>
        protected SnPatch Patch(string id, string fromTo, string version, Dependency[] dependencies)
        {
            var boundary = ParseBoundary(fromTo);
            return new SnPatch
            {
                Id = id,
                Version = Version.Parse(version),
                Boundary = new VersionBoundary
                {
                    MinVersion = boundary.min,
                    MinVersionIsExclusive = boundary.minEx,
                    MaxVersion = boundary.max,
                    MaxVersionIsExclusive = boundary.maxEx
                },
                Dependencies = dependencies
            };
        }
        /// <summary>
        /// Creates a Dependency
        /// </summary>
        /// <param name="id">ComponentId</param>
        /// <param name="fromTo">Complex source version. Example: "1.1 &lt;= v &lt;= 1.1"</param>
        /// <returns></returns>
        protected Dependency Dep(string id, string fromTo)
        {
            var boundary = ParseBoundary(fromTo);
            return new Dependency
            {
                Id = id,
                Boundary = new VersionBoundary
                {
                    MinVersion = boundary.min,
                    MinVersionIsExclusive = boundary.minEx,
                    MaxVersion = boundary.max,
                    MaxVersionIsExclusive = boundary.maxEx
                }
            };
        }

        protected (Version min, bool minEx, Version max,  bool maxEx) ParseBoundary(string src)
        {
            var a = src.Split('v').Select(x => x.Trim()).ToArray();

            var minEx = a[0][0] != '=';
            var min = Version.Parse(a[0].Substring(1));
            var maxEx = false;
            var max = default(Version);
            if (a.Length > 1)
            {
                maxEx = a[1][0] != '=';
                max = Version.Parse(a[1].Substring(1));
            }
            return (min, minEx, max, maxEx);
        }
    }
}

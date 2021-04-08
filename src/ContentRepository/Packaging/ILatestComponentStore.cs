using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Storage;

namespace SenseNet.ContentRepository.Packaging
{
    public interface ILatestComponentStore
    {
        /// <summary>
        /// Returns latest releases
        /// </summary>
        /// <returns>A set of ReleaseInfos.</returns>
        Task<IEnumerable<ReleaseInfo>> GetLatestReleases(CancellationToken cancel);
        /// <summary>
        /// Returns all latest component versions.
        /// The value is provided from a central storage.
        /// </summary>
        /// <returns>String-version pairs of the ComponentIds and Versions.</returns>
        Task<IDictionary<string, Version>> GetLatestComponentVersions(CancellationToken cancel);
    }

    public class DefaultLatestComponentStore : ILatestComponentStore
    {
        public Task<IEnumerable<ReleaseInfo>> GetLatestReleases(CancellationToken cancel)
        {
            return System.Threading.Tasks.Task.FromResult<IEnumerable<ReleaseInfo>>(new ReleaseInfo[0]);
        }
        public Task<IDictionary<string, Version>> GetLatestComponentVersions(CancellationToken cancel)
        {
            return System.Threading.Tasks.Task.FromResult<IDictionary<string, Version>>(new Dictionary<string, Version>());
        }
    }

}

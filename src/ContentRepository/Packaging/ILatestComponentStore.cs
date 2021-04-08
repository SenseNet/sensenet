using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Packaging
{
    public interface ILatestComponentStore
    {
        /// <summary>
        /// Returns latest version of the component identified by the given <paramref name="componentId"/>.
        /// The value is provided from a central storage.
        /// </summary>
        /// <param name="componentId">Id of the component.</param>
        /// <returns><see cref="Version"/> or null.</returns>
        Version GetLatestVersion(string componentId);
    }
    public class DefaultLatestComponentStore : ILatestComponentStore
    {
        public Version GetLatestVersion(string componentId)
        {
            return null;
        }
    }

}

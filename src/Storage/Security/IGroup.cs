using SenseNet.Security;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Defines an interface for a group of users or groups.
    /// </summary>
    public interface IGroup : ISecurityContainer
    {
        /// <summary>
        /// Gets collection of <see cref="Node"/>s that represents the member users or other groups.
        /// </summary>
        IEnumerable<Node> Members { get; }
    }
}
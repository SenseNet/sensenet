using SenseNet.Security;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Defines an interface for representing a group of users or additional groups.
    /// </summary>
    public interface IGroup : ISecurityContainer
    {
        /// <summary>
        /// Gets collection of <see cref="Node"/> that represents the member users or additional groups.
        /// </summary>
        IEnumerable<Node> Members { get; }
    }
}
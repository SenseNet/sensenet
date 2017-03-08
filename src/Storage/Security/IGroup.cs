using SenseNet.Security;
using System.Collections.Generic;

namespace SenseNet.ContentRepository.Storage.Security
{
    public interface IGroup : ISecurityContainer
    {
        IEnumerable<Node> Members { get; }
    }
}
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    public interface IFolder
    {
        IEnumerable<Node> Children { get; }
        int ChildCount { get; }

        QueryResult GetChildren(QuerySettings settings);
        QueryResult GetChildren(string text, QuerySettings settings);
    }
}
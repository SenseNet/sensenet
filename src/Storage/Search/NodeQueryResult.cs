using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Search
{
    [Obsolete("Use SenseNet.Search.QueryResult instead.", true)]
    public class NodeQueryResult
    {
        private NodeList<Node> _result;

        public IEnumerable<Node> Nodes { get { return _result; } }
        public IEnumerable<int> Identifiers { get { return _result.GetIdentifiers(); } }
        public int Count { get { return _result.Count; } }

        internal NodeQueryResult(NodeList<Node> nodes)
        {
            _result = nodes;
        }

        // public constructor for serving in-memory nodequery result creation
        public NodeQueryResult(IEnumerable<int> ids) : this(new NodeList<Node>(ids))
        {
        }
    }
}

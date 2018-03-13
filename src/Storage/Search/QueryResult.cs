using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search
{
    /// <summary>
    /// Defines a class for result of content queries.
    /// </summary>
    public class QueryResult
    {
        private class NodeResolver : INodeResolver<Node>
        {
            private readonly Node[] _nodes;

            public NodeResolver(Node[] nodes)
            {
                _nodes = nodes;
            }
            public NodeResolver(IEnumerable<Node> nodes)
            {
                _nodes = nodes.ToArray();
            }

            public int IdCount => _nodes.Length;

            public int GetPermittedCount()
            {
                return _nodes.Length;
            }
            public IEnumerable<int> GetIdentifiers()
            {
                return _nodes.Select(n => n.Id).ToArray();
            }
            public IEnumerable<Node> GetPage(int skip, int top)
            {
                var availCount = _nodes.Length - skip;
                if (availCount <= 0)
                    return new Node[0];
                if (availCount < top)
                    top = availCount;
                var result = new Node[availCount];
                Array.Copy(_nodes, skip, result, 0, top);
                return result;
            }
            public IEnumerator<Node> GetEnumerator()
            {
                return ((IEnumerable<Node>)_nodes).GetEnumerator();
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Represents empty result.
        /// </summary>
        public static readonly QueryResult Empty = new QueryResult(new int[0]);

        private readonly INodeResolver<Node> _result;

        /* =========================================== */

        /// <summary>
        /// Initializes a new instance of the QueryResult with identifiers as hit collection.
        /// </summary>
        /// <param name="result">The IEnumerable&lt;int&gt; that contains ids of hits.</param>
        public QueryResult(IEnumerable<int> result)
        {
            _result = new NodeList<Node>(result);
            Count = _result.IdCount;
        }

        /// <summary>
        /// Initializes a new instance of the QueryResult with
        /// identifiers as hit collection and it's count.
        /// </summary>
        /// <param name="result">The IEnumerable&lt;int&gt; that contains ids of hits.</param>
        /// <param name="totalCount">Count of items.</param>
        public QueryResult(IEnumerable<int> result, int totalCount)
        {
            _result = new NodeList<Node>(result);
            Count = totalCount;
        }

        /// <summary>
        /// Initializes a new instance of the QueryResult with a prepared hit collection.
        /// Use this constructor if the hit collection contains all items in the memory.
        /// </summary>
        /// <param name="nodes">The <see cref="IEnumerable&lt;Node&gt;"/> that contains hits.</param>
        public QueryResult(IEnumerable<Node> nodes)
        {
            switch (nodes)
            {
                case NodeList<Node> nodeList:
                    _result = nodeList;
                    Count = nodeList.Count;
                    break;
                case List<Node> listOfNodes:
                    _result = new NodeResolver(listOfNodes);
                    Count = listOfNodes.Count;
                    break;
                default:
                    var nodeArray = nodes as Node[] ?? nodes.ToArray();
                    _result = new NodeResolver(nodeArray);
                    Count = nodeArray.Length;
                    break;
            }
        }

        /// <summary>
        /// Initializes a new instance of the QueryResult with a prepared hit collection and it's count.
        /// Use this constructor if the hit collection does contain all items in the memory and uses a block-read algorithm.
        /// </summary>
        /// <param name="nodes">The <see cref="IEnumerable&lt;Node&gt;"/> that contains hits.</param>
        /// <param name="totalCount">Count of items.</param>
        public QueryResult(IEnumerable<Node> nodes, int totalCount)
        {
            _result = new NodeResolver(nodes);
            Count = totalCount;
        }

        // =========================================== Properties

        /// <summary>
        /// Gets the identifiers of found nodes.
        /// </summary>
        public IEnumerable<int> Identifiers => _result.GetIdentifiers();

        /// <summary>
        /// Gets the found nodes.
        /// </summary>
        public IEnumerable<Node> Nodes => _result;

        /// <summary>
        /// Gets the total count of permitted items without top and skip
        /// restrictions. The actual count is available on the Nodes and
        /// Identifiers properties but due to performance considerations,
        /// it is strongly recommended to use only Identifiers.Count().
        /// </summary>
        public int Count { get; }
    }
}

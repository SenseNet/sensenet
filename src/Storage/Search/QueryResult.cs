using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using System.Linq;
using System.Diagnostics;

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
            private Node[] _nodes;

            public NodeResolver(IEnumerable<Node> nodes)
            {
                _nodes = nodes.ToArray();
            }

            public int IdCount
            {
                get { return _nodes.Length; }
            }
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
        /// <param name="totalCount">Count os items.</param>
        public QueryResult(IEnumerable<int> result, int totalCount)
        {
            _result = new NodeList<Node>(result);
            Count = totalCount;
        }

        /// <summary>
        /// Initializes a new instance of the QueryResult with
        /// a prepared hit collection.
        /// </summary>
        /// <param name="nodes">The <see cref="IEnumerable&lt;Node&gt;"/> that contains hits.</param>
        public QueryResult(IEnumerable<Node> nodes)
        {
            _result = new NodeResolver(nodes);
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
        /// Gets the count of items.
        /// </summary>
        public int Count { get; private set; }
    }
}

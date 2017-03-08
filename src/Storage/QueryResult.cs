using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using System.Linq;
using System.Diagnostics;

namespace SenseNet.Search
{
    public class QueryResult
    {
        private class _NodeResolver : INodeResolver<Node>
        {
            private Node[] _nodes;

            public _NodeResolver(IEnumerable<Node> nodes)
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

        public static readonly QueryResult Empty = new QueryResult(new int[0]);

        private INodeResolver<Node> _result;

        // =========================================== 

        /// <summary>
        /// For full id list
        /// </summary>
        /// <param name="result"></param>
        public QueryResult(IEnumerable<int> result)
        {
            _result = new NodeList<Node>(result);
            Count = _result.IdCount;
        }

        /// <summary>
        /// For query
        /// </summary>
        /// <param name="result"></param>
        /// <param name="totalCount"></param>
        public QueryResult(IEnumerable<int> result, int totalCount)
        {
            _result = new NodeList<Node>(result);
            Count = totalCount;
        }

        /// <summary>
        /// For dynamic contents
        /// </summary>
        /// <param name="nodes"></param>
        public QueryResult(IEnumerable<Node> nodes)
        {
            _result = new _NodeResolver(nodes);
        }

        // =========================================== Properties

        public IEnumerable<int> Identifiers
        {
            get { return _result.GetIdentifiers(); }
        }

        public IEnumerable<Node> Nodes { get { return _result; } }

        /// <summary>
        /// Total count of contents
        /// </summary>
        public int Count { get; private set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Storage
{
    public class NodeEnumerator : IEnumerable<Node>, IEnumerator<Node>
    {
        public static IEnumerable<Node> GetNodes(string path)
        {
            return GetNodes(path, ExecutionHint.None);
        }
        public static IEnumerable<Node> GetNodes(string path, ExecutionHint hint)
        {
            return GetNodes(path, hint, null, null);
        }
        public static IEnumerable<Node> GetNodes(string path, ExecutionHint hint, NodeQuery filter, int? depth)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            return new NodeEnumerator(path, hint, filter, depth);
        }

        // ================================================================== 

        protected readonly string RootPath;
        private Stack<int[]> _currentLevel;
        private Stack<int> _currentIndices;
        protected Node CurrentNode;
        private readonly ExecutionHint _hint;
        private readonly NodeQuery _filter;
        private int? _depth;
        private bool _skip;

        protected NodeEnumerator(string path, ExecutionHint executionHint, NodeQuery filter, int? depth)
        {
            RootPath = path;
            _currentLevel = new Stack<int[]>();
            _currentIndices = new Stack<int>();
            _hint = executionHint;
            _filter = filter;
            _depth = depth.HasValue ? Math.Max(1, depth.Value) : depth;
        }

        // ================================================================== IEnumerable<Node> Members

        public IEnumerator<Node> GetEnumerator()
        {
            return this;
        }

        // ================================================================== IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        // ================================================================== IDisposable Members

        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _currentLevel = null;
            _currentIndices = null;
            CurrentNode = null;
            _disposed = true;
        }

        // ================================================================== IEnumerator<Node> Members

        public Node Current
        {
            get
            {
                if (CurrentNode == null)
                    throw new InvalidOperationException("Use MoveNext before calling Current");
                return CurrentNode;
            }
        }

        // ================================================================== IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }
        public bool MoveNext()
        {
            if (CurrentNode == null)
                return MoveToFirst();
            if (MoveToFirstChild())
                return true;
            if (MoveToNextSibling())
                return true;
            while (true)
            {
                if (MoveToParent())
                    if (MoveToNextSibling())
                        return true;
                if (_currentLevel.Count == 0)
                    break;
            }
            return false;
        }
        public void Reset()
        {
            _currentLevel.Clear();
            _currentIndices.Clear();
            CurrentNode = null;
        }

        // ================================================================== Tools

        private NodeQueryResult QueryChildren(int thisId)
        {
            switch (_hint)
            {
                case ExecutionHint.None:
                    if (StorageContext.Search.IsOuterEngineEnabled && StorageContext.Search.SearchEngine != InternalSearchEngine.Instance)
                        return QueryChildrenFromLucene(thisId);
                    return QueryChildrenFromDatabase(thisId);
                case ExecutionHint.ForceRelationalEngine:
                    return QueryChildrenFromDatabase(thisId);
                case ExecutionHint.ForceIndexedEngine:
                    return QueryChildrenFromLucene(thisId);
                default:
                    throw new SnNotSupportedException();
            }
        }
        protected virtual NodeQueryResult QueryChildrenFromLucene(int thisId)
        {
            var r = StorageContext.Search.ContentRepository.ExecuteContentQuery($"+ParentId:{thisId}", QuerySettings.AdminSettings);
            return new NodeQueryResult(r.Identifiers);
        }
        private NodeQueryResult QueryChildrenFromDatabase(int thisId)
        {
            if (_filter != null)
                throw new NotSupportedException("Cannot query the children from database with filter.");
            var idArray = DataProvider.Current.GetChildrenIdentfiers(thisId);
            return new NodeQueryResult(idArray);
        }

        private Node LoadCurrentNode()
        {
            var level = _currentLevel.Peek();
            var index = _currentIndices.Peek();

            try
            {
                _skip = false;

                return Node.LoadNode(level[index]);
            }
            catch (SenseNetSecurityException)
            {
                // access denied to current node --> move on
                _skip = true;

                return null;
            }
        }

        protected virtual bool MoveToFirst()
        {
            var node = Node.LoadNode(RootPath);
            if (node == null)
                return false;
            _currentLevel.Push(new[] { node.Id });
            _currentIndices.Push(0);
            CurrentNode = node;
            return true;
        }

        protected virtual bool MoveToFirstChild()
        {
            if (_depth.HasValue && _currentLevel.Count == _depth.Value)
                return false;

            var children = QueryChildren(_currentLevel.Peek()[_currentIndices.Peek()]);
            if (children.Count == 0)
                return false;

            _currentLevel.Push(children.Identifiers.ToArray());
            _currentIndices.Push(0);
            CurrentNode = LoadCurrentNode();

            if (_skip)
                return MoveToNextSibling();
            return true;
        }

        protected virtual bool MoveToNextSibling()
        {
            var level = _currentLevel.Peek();
            var index = _currentIndices.Peek() + 1;
            if (index >= level.Length)
                return false;
            _currentIndices.Pop();
            _currentIndices.Push(index);
            CurrentNode = LoadCurrentNode();

            while (_skip)
            {
                // we have to step back here because
                // MoveToNextSibling increments the index
                var moveNext = MoveToNextSibling();
                if (!moveNext)
                    return false;
            }

            return true;
        }

        protected virtual bool MoveToParent()
        {
            if (_currentLevel.Count == 0)
                return false;
            _currentLevel.Pop();
            _currentIndices.Pop();

            if (_currentLevel.Count == 0)
                return false;
            
            CurrentNode = LoadCurrentNode();

            return true;
        }
    }
}
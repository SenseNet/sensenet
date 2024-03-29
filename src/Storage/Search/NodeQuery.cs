using System;
using SenseNet.ContentRepository.Storage.Data;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Schema;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Storage.Search
{
    [System.Diagnostics.DebuggerDisplay("{Name} = {Value}")]
    internal class NodeQueryParameter //TODO: Part of 'CQL to SQL compiler' for future use.
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    /// <summary>
    /// Contains static methods to execute queries when the outer search engine is not available.
    /// This is typically occurs during in the system start sequence.
    /// Do not use these methods when the outer search engine is running.
    /// </summary>
    public static class NodeQuery
    {
        private static IDataStore DataStore => Providers.Instance.DataStore;

        /// <summary>
        /// Returns with the count of the stored instances by the given <see cref="NodeType"/>.
        /// CQL equivalent: if exactType is true: 'Type:{nodeTypeName} .COUNTONLY' otherwise: 'TypeIs:{nodeTypeName} .COUNTONLY'.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/> that specifies the allowed type of nodes in the result.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <returns>Count of instances.</returns>
        public static int InstanceCount(NodeType nodeType, bool exactType)
        {
            return DataStore.InstanceCountAsync(exactType ? new[] {nodeType.Id} : nodeType.GetAllTypes().ToIdArray(), CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns the children of the node identified by the given path.
        /// CQL equivalent: 'InFolder:{path}'.
        /// </summary>
        /// <param name="parentPath">Defines the root node.</param>
        /// <exception cref="ArgumentNullException">If the given parentPath is null.</exception>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryChildren(string parentPath)
        {
            if (parentPath == null)
                throw new ArgumentNullException(nameof(parentPath));

            var head = NodeHead.Get(parentPath);

            // parent does not exist
            if (head == null)
                return new QueryResult(new int[0]);

            var ids = DataStore.GetChildrenIdentifiersAsync(head.Id, CancellationToken.None).GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }

        /// <summary>
        /// Returs with the children of the node identified by the given parentId.
        /// CQL equivalent: 'ParentId:{parentId}'.
        /// </summary>
        /// <param name="parentId">The id of the root node.</param>
        /// <exception cref="InvalidOperationException">If the parentId is 0.</exception>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryChildren(int parentId)
        {
            if (parentId <= 0)
                throw new InvalidOperationException("Parent node is not saved");

            var ids = DataStore.GetChildrenIdentifiersAsync(parentId, CancellationToken.None).GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }

        /// <summary>
        /// Return with nodes that are in the subtree of the node identified by the pathStart.
        /// Note that the pathStart will be suffixed with the trailing slash ('/') if it does not ends with it.
        /// Similar CQL without ordering: 'InTree:{pathStart}'.
        /// Note that the result of the CQL example contains the root but the result of method does not.
        /// </summary>
        /// <param name="pathStart">The path of the root node.</param>
        /// <param name="orderByPath">Specifies that the result will be sorted or not.</param>
        /// <exception cref="ArgumentNullException">If the given pathStart is null.</exception>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByPath(string pathStart, bool orderByPath)
        {
            if (pathStart == null)
                throw new ArgumentNullException(nameof(pathStart));
            IEnumerable<int> ids = DataStore.QueryNodesByPathAsync(pathStart, orderByPath, CancellationToken.None)
                .GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// Returns with the nodes by the given <see cref="NodeType"/>.
        /// CQL equivalent: if exactType is true: 'Type:{nodeTypeName}' otherwise: 'TypeIs:{nodeTypeName}'.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/> that specifies the allowed type of nodes in the result.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <exception cref="ArgumentNullException">If the given nodeType is null.</exception>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByType(NodeType nodeType, bool exactType)
        {
            if (nodeType == null)
                throw new ArgumentNullException(nameof(nodeType));
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataStore.QueryNodesByTypeAsync(typeIds, CancellationToken.None).GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// Returns with the nodes by the given <see cref="NodeType"/> and name.
        /// CQL equivalent: if exactType is true: '+Name:{name} +Type:{nodeTypeName}'
        /// otherwise: '+Name:{name} +TypeIs:{nodeTypeName}'.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/> that specifies the allowed type of nodes in the result.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <param name="name">The name of the node. If null, the name filter will be not applied.</param>
        /// <exception cref="ArgumentNullException">If the given nodeType is null.</exception>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByTypeAndName(NodeType nodeType, bool exactType, string name)
        {
            if (nodeType == null)
                throw new ArgumentNullException(nameof(nodeType));
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataStore.QueryNodesByTypeAndPathAndNameAsync(typeIds, (string[]) null, false, name, CancellationToken.None)
                .GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// Return with nodes that are in the subtree of the node identified by the pathStart filtered by the given <see cref="NodeType"/>.
        /// Note that the pathStart will be suffixed with the trailing slash ('/') if it does not ends with it.
        /// Similar CQL without ordering: if exactType is true: '+InTree:{pathStart} +Type:{nodeTypeName}'
        /// otherwise: '+InTree:{pathStart} +TypeIs:{nodeTypeName}'.
        /// Note that the result of the CQL example contains the root but the result of method does not.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/> that specifies the allowed type of nodes in the result.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <param name="pathStart">The path of the root node.</param>
        /// <param name="orderByPath">Specifies that the result will be sorted or not.</param>
        /// <exception cref="ArgumentNullException">If the given nodeType or pathStart is null.</exception>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByTypeAndPath(NodeType nodeType, bool exactType, string pathStart, bool orderByPath)
        {
            if (nodeType == null)
                throw new ArgumentNullException(nameof(nodeType));
            if (pathStart == null)
                throw new ArgumentNullException(nameof(pathStart));
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataStore.QueryNodesByTypeAndPathAsync(typeIds, pathStart, orderByPath, CancellationToken.None)
                .GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// Return with nodes that are in the subtrees of the nodes identified by the pathStarts filtered by the given <see cref="NodeType"/>.
        /// Note that every pathStart will be suffixed with the trailing slash ('/') if it does not ends with it.
        /// Similar CQL without ordering and only one path: if exactType is true: '+InTree:{pathStart} +Type:{nodeTypeName}'
        /// otherwise: '+InTree:{pathStart} +TypeIs:{nodeTypeName}'.
        /// Note that the result of the CQL example contains the root but the result of method does not.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/> that specifies the allowed type of nodes in the result.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <param name="pathStart">One or more path of the root nodes.</param>
        /// <param name="orderByPath">Specifies that the result will be sorted or not.</param>
        /// <exception cref="ArgumentNullException">If the given nodeType or pathStart is null.</exception>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByTypeAndPath(NodeType nodeType, bool exactType, string[] pathStart, bool orderByPath)
        {
            if (nodeType == null)
                throw new ArgumentNullException(nameof(nodeType));
            if (pathStart == null)
                throw new ArgumentNullException(nameof(pathStart));
            var typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataStore.QueryNodesByTypeAndPathAsync(typeIds, pathStart, orderByPath, CancellationToken.None)
                .GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// Return with nodes that are in the subtree of the node identified by the pathStart filtered by the given <see cref="NodeType"/> and name.
        /// Note that the pathStart will be suffixed with the trailing slash ('/') if it does not ends with it.
        /// Similar CQL without ordering: if exactType is true: '+InTree:{pathStart} +Type:{nodeTypeName} +Name:{name}'
        /// otherwise: '+InTree:{pathStart} +TypeIs:{nodeTypeName} +Name:{name}'.
        /// Note that the result of the CQL example contains the root but the result of method does not.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/> that specifies the allowed type of nodes in the result.
        /// If null, the type filter is not applied.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <param name="pathStart">The path of the root node.</param>
        /// <param name="orderByPath">Specifies that the result will be sorted or not.</param>
        /// <param name="name">The name of the node. If null, the name filter will be not applied.</param>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByTypeAndPathAndName(NodeType nodeType, bool exactType, string pathStart, bool orderByPath, string name)
        {
            int[] typeIds = null;
            if (nodeType != null)
                typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();
            IEnumerable<int> ids = DataStore.QueryNodesByTypeAndPathAndNameAsync(typeIds, pathStart, orderByPath, name, CancellationToken.None)
                .GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// Return with nodes that are in the subtree of the node identified by the pathStart filtered by the given <see cref="IEnumerable&lt;NodeType&gt;"/> and name.
        /// Note that the pathStart will be suffixed with the trailing slash ('/') if it does not ends with it.
        /// Similar CQL without ordering: if exactType is true: '+InTree:{pathStart} +Type:{nodeTypeName} +Name:{name}'
        /// otherwise: '+InTree:{pathStart} +TypeIs:{nodeTypeName} +Name:{name}'.
        /// Note that the result of the CQL example contains the root but the result of method does not.
        /// </summary>
        /// <param name="nodeTypes">The <see cref="IEnumerable&lt;NodeType&gt;"/> that specifies the allowed types of nodes in the result.
        /// If null, the type filter is not applied.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <param name="pathStart">The path of the root node.</param>
        /// <param name="orderByPath">Specifies that the result will be sorted or not.</param>
        /// <param name="name">The name of the node. If null, the name filter will be not applied.</param>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByTypeAndPathAndName(IEnumerable<NodeType> nodeTypes, bool exactType, string pathStart, bool orderByPath, string name)
        {
            int[] typeIds = null;
            if (nodeTypes != null)
            {
                var idList = new List<int>();

                if (exactType)
                {
                    idList = nodeTypes.Select(nt => nt.Id).ToList();
                }
                else
                {
                    foreach (var nodeType in nodeTypes)
                    {
                        idList.AddRange(nodeType.GetAllTypes().ToIdArray());
                    }
                }

                if (idList.Count > 0)
                    typeIds = idList.ToArray();
            }

            var ids = DataStore.QueryNodesByTypeAndPathAndNameAsync(typeIds, pathStart, orderByPath, name, CancellationToken.None)
                .GetAwaiter().GetResult();
            return new QueryResult(new NodeList<Node>(ids));
        }

        /// <summary>
        /// Return with nodes that are in the subtree of the node identified by the pathStart filtered by the given <see cref="NodeType"/> and properties.
        /// Note that the pathStart will be suffixed with the trailing slash ('/') if it does not ends with it.
        /// Note that the result does not contain the root.
        /// Every result node need to meet every property filter.
        /// Every property filter has a name-value pair and an <see cref="Operator"/>.
        /// By the filtering concept, the property predicates are linked with AND logical operator.
        /// Logical OR cannot be applied in this case.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/> that specifies the allowed type of nodes in the result.</param>
        /// <param name="exactType">True if inherited-type nodes are not in the result.</param>
        /// <param name="pathStart">The path of the root node.</param>
        /// <param name="orderByPath">Specifies that the result will be sorted or not.</param>
        /// <param name="properties">A <see cref="List&lt;QueryPropertyData&gt;"/> containing property predicates.</param>
        /// <returns>A <see cref="QueryResult"/> instance.</returns>
        public static QueryResult QueryNodesByTypeAndPathAndProperty(NodeType nodeType, bool exactType, string pathStart, bool orderByPath, List<QueryPropertyData> properties)
        {
            int[] typeIds = null;
            if (nodeType != null)
                typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();

            var ids = DataStore.QueryNodesByTypeAndPathAndPropertyAsync(typeIds, pathStart, orderByPath, properties, CancellationToken.None)
                .GetAwaiter().GetResult();

            return new QueryResult(new NodeList<Node>(ids));
        }
        /// <summary>
        /// Returns with the nodes that references the given node.
        /// CQL equivalent: '{referenceName}:{referredNodeId}'.
        /// </summary>
        public static QueryResult QueryNodesByReference(string referenceName, int referredNodeId)
        {
            return QueryNodesByReferenceAndType(referenceName, referredNodeId, null, false);
        }
        /// <summary>
        /// Returns with the nodes that references the given node. The result is filtered by the given type or type family.
        /// CQL equivalent: '+Type:{nodeTypeName} +{referenceName}:{referredNodeId}' or '+TypeIs:{nodeTypeName} +{referenceName}:{referredNodeId}'.
        /// </summary>
        public static QueryResult QueryNodesByReferenceAndType(string referenceName, int referredNodeId, NodeType nodeType, bool exactType)
        {
            int[] typeIds = null;
            if (nodeType != null)
                typeIds = exactType ? new[] { nodeType.Id } : nodeType.GetAllTypes().ToIdArray();

            var ids = DataStore.QueryNodesByReferenceAndTypeAsync(referenceName, referredNodeId, typeIds, CancellationToken.None)
                .GetAwaiter().GetResult();

            return new QueryResult(new NodeList<Node>(ids));
        }
    }
}

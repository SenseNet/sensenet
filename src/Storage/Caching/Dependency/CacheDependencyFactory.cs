﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Factory class for creating cache dependencies.
    /// </summary>
    public static class CacheDependencyFactory
    {
        internal static CacheDependency CreateBinaryDataDependency(int nodeId, string path, int nodeTypeId)
        {
            var aggregateCacheDependency = new AggregateCacheDependency();
            aggregateCacheDependency.Add(
                new NodeIdDependency(nodeId),
                new PathDependency(path),
                new NodeTypeDependency(nodeTypeId)
            );

            return aggregateCacheDependency;
        }

        internal static CacheDependency CreateNodeHeadDependency(NodeHead nodeHead)
        {
            if (nodeHead == null)
                throw new ArgumentNullException(nameof(nodeHead), "NodeHead cannot be null.");

            var aggregateCacheDependency = new AggregateCacheDependency();

            aggregateCacheDependency.Add(
                new NodeIdDependency(nodeHead.Id),
                new PathDependency(nodeHead.Path)
                );

            return aggregateCacheDependency;
        }

        private static CacheDependency CreateNodeDependency(int id, string path, int nodeTypeId)
        {
            var aggregateCacheDependency = new AggregateCacheDependency();

            aggregateCacheDependency.Add(
                new NodeIdDependency(id),
                new PathDependency(path),
                new NodeTypeDependency(nodeTypeId)
                );

            return aggregateCacheDependency;
        }

        /// <summary>
        /// Creates a cache dependency based on a node head.
        /// </summary>
        public static CacheDependency CreateNodeDependency(NodeHead nodeHead)
        {
            if (nodeHead == null)
                throw new ArgumentNullException(nameof(nodeHead), "NodeHead cannot be null.");

            return CreateNodeDependency(nodeHead.Id, nodeHead.Path, nodeHead.NodeTypeId);
        }

        /// <summary>
        /// Creates a cache dependency based on a node.
        /// </summary>
        public static CacheDependency CreateNodeDependency(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node), "Node cannot be null.");

            return CreateNodeDependency(node.Id, node.Path, node.NodeTypeId);
        }

        // ContentListTypeId 
        // BinaryPropertyTypeIds 
        internal static CacheDependency CreateNodeDataDependency(NodeData nodeData)
        {
            if (nodeData == null)
                throw new ArgumentNullException(nameof(nodeData));

            var aggregateCacheDependency = new AggregateCacheDependency();

            aggregateCacheDependency.Add(
                new NodeIdDependency(nodeData.Id),
                new PathDependency(nodeData.Path),
                new NodeTypeDependency(nodeData.NodeTypeId)
            );

            // Add cache dependency for resource files, if node data contains string resource keys.
            if (SR.ResourceManager.Running)
            {
                var resourcePaths = new List<string>();

                // deal with string properties only
                foreach (var propertyType in nodeData.PropertyTypes.Where(propertyType => propertyType.DataType == Schema.DataType.String))
                {
                    AddResourceDependencyIfNeeded(aggregateCacheDependency, nodeData.GetDynamicRawData(propertyType) as string, resourcePaths);
                }

                // display name is not a dynamic raw data, add it separately
                AddResourceDependencyIfNeeded(aggregateCacheDependency, nodeData.DisplayName, resourcePaths);
            }

            return aggregateCacheDependency;
        }

        private static void AddResourceDependencyIfNeeded(AggregateCacheDependency aggregateCacheDependency, string propertyValue, ICollection<string> addedResourcePaths)
        {
            if (!SR.ResourceManager.ParseResourceKey(propertyValue, out var className, out _)) 
                return;

            foreach (var resHead in SR.ResourceManager.GetResourceFilesForClass(className)
                .Where(resPath => !addedResourcePaths.Contains(resPath))
                .Select(NodeHead.Get)
                .Where(h => h != null))
            {
                aggregateCacheDependency.Add(
                    new NodeIdDependency(resHead.Id),
                    new PathDependency(resHead.Path));

                addedResourcePaths.Add(resHead.Path);
            }
        }
    }
}
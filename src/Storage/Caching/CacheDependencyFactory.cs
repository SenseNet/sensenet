using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;

namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    public static class CacheDependencyFactory
    {


        internal static CacheDependency CreateNodeHeadDependency(NodeHead nodeHead)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead", "NodeHead cannot be null.");

            var aggregateCacheDependency = new System.Web.Caching.AggregateCacheDependency();

            aggregateCacheDependency.Add(
                new NodeIdDependency(nodeHead.Id),
                new PathDependency(nodeHead.Path)
                );

            return aggregateCacheDependency;
        }

        private static CacheDependency CreateNodeDependency(int id, string path, int nodeTypeId)
        {
            var aggregateCacheDependency = new System.Web.Caching.AggregateCacheDependency();

            aggregateCacheDependency.Add(
                new NodeIdDependency(id),
                new PathDependency(path),
                new NodeTypeDependency(nodeTypeId)
                );

            return aggregateCacheDependency;
        }

        public static CacheDependency CreateNodeDependency(NodeHead nodeHead)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead", "NodeHead cannot be null.");

            return CreateNodeDependency(nodeHead.Id, nodeHead.Path, nodeHead.NodeTypeId);
        }

        public static CacheDependency CreateNodeDependency(Node node)
        {
            if (node == null)
                throw new ArgumentNullException("node", "Node cannot be null.");

            return CreateNodeDependency(node.Id, node.Path, node.NodeTypeId);
        }

        // ContentListTypeId 
        // BinaryPropertyTypeIds 
        internal static CacheDependency CreateNodeDataDependency(NodeData nodeData)
        {
            if (nodeData == null)
                throw new ArgumentNullException("nodeData");

            var aggregateCacheDependency = new System.Web.Caching.AggregateCacheDependency();

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
            string className, resKey;
            if (!SR.ResourceManager.ParseResourceKey(propertyValue, out className, out resKey)) 
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

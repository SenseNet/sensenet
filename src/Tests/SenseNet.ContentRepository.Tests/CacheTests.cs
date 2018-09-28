using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Caching.Legacy;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Tests;
// ReSharper disable UnusedVariable

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class CacheTests : TestBase
    {
        #region private class TestAspNetCache...
        private class TestAspNetCache : CacheBase
        {
            private readonly AspNetCache _cache;
            public StringBuilder Log { get; } = new StringBuilder();

            public TestAspNetCache()
            {
                _cache = new AspNetCache();
            }

            public override object Get(string key)
            {
                Log.AppendLine($"Get: {key}");
                return _cache.Get(key);
            }

            public override void Insert(string key, object value)
            {
                _cache.Insert(key, value);
                Log.AppendLine($"Insert: {key}");
            }

            public override void Insert(string key, object value, CacheDependency dependencies)
            {
                _cache.Insert(key, value, dependencies);
                Log.AppendLine($"Insert: {key}, {dependencies.GetType().Name}");
            }

            public override void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration,
                TimeSpan slidingExpiration, object onRemoveCallback)
            {
                _cache.Insert(key, value, dependencies, absoluteExpiration, slidingExpiration, onRemoveCallback);
                Log.AppendLine($"Insert: {key}, {dependencies.GetType().Name}, ....");
            }
            [Obsolete("Do not use priority in the caching API. Use the expiration times instead.")]
            public override void Insert(string key, object value, CacheDependency dependencies, DateTime absoluteExpiration,
                TimeSpan slidingExpiration, CacheItemPriority priority, object onRemoveCallback)
            {
                _cache.Insert(key, value, dependencies, absoluteExpiration, slidingExpiration, priority, onRemoveCallback);
                Log.AppendLine($"Insert: {key}, {dependencies.GetType().Name}, ....");
            }

            public override void Remove(string key)
            {
                _cache.Remove(key);
                Log.AppendLine($"Remove: {key}");
            }

            public override void Reset()
            {
                _cache.Reset();
                Log.AppendLine("Reset");
            }

            public override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _cache.GetEnumerator();
            }

            public override int Count => _cache.Count;

            public override long EffectivePercentagePhysicalMemoryLimit =>
                _cache.EffectivePercentagePhysicalMemoryLimit;

            public override long EffectivePrivateBytesLimit => _cache.EffectivePrivateBytesLimit;

            public override object this[string key]
            {
                get
                {
                    Log.AppendLine($"this.get: {key}");
                    return _cache[key];}
                set
                {
                    _cache[key] = value;
                    Log.AppendLine($"this.set: {key}");
                }
            }
        }
        #endregion

        [TestMethod]
        public void Cache_Legacy_DeleteParent_SubtreeRemoved()
        {
            Test((builder) => { builder.UseCacheProvider(new TestAspNetCache()); },
                () =>
                {
                    // create structure
                    var root = Repository.Root;
                    var rootHead = NodeHead.Get(root.Id);
                    var testRoot = CreateTestRoot();
                    var testRootHead = NodeHead.Get(testRoot.Id);
                    var file = new File(testRoot) {Name = "file"};
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString("Lorem ipsum..."));
                    file.Save();
                    var fileHead = NodeHead.Get(file.Id);
                    var binaryData = file.Binary;

                    // reset cache
                    DistributedApplication.Cache.Reset();

                    root = Node.Load<PortalRoot>(root.Id);
                    testRoot = Node.Load<SystemFolder>(testRoot.Id);
                    file = Node.Load<File>(file.Id);
                    var stream = file.Binary.GetStream();

                    Assert.IsTrue(IsInCache(root.Data));
                    Assert.IsTrue(IsInCache(rootHead));
                    Assert.IsTrue(IsInCache(testRoot.Data));
                    Assert.IsTrue(IsInCache(testRootHead));
                    Assert.IsTrue(IsInCache(file.Data));
                    Assert.IsTrue(IsInCache(fileHead));
                    Assert.IsTrue(IsInCache(binaryData));

                    //var keysBefore = cache.WhatIsInTheCache();

                    testRoot.ForceDelete();

                    Assert.IsTrue(IsInCache(root.Data));
                    Assert.IsTrue(IsInCache(rootHead));
                    Assert.IsFalse(IsInCache(testRoot.Data));
                    Assert.IsFalse(IsInCache(testRootHead));
                    Assert.IsFalse(IsInCache(file.Data));
                    Assert.IsFalse(IsInCache(fileHead));
                    Assert.IsFalse(IsInCache(binaryData));
                });
        }

        [TestMethod]
        public void Cache_Legacy_NodeIdDependency_Private()
        {
            Test((builder) => { builder.UseCacheProvider(new TestAspNetCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;
                    cache.Reset();

                    // create node1 and cache it with it own NodeIdDependency
                    var node1 = CreateTestRoot();
                    var nodeId1 = node1.Id;
                    var nodeData1 = node1.Data;
                    var key1 = "NodeData." + node1.VersionId;
                    var dependencies1 = new NodeIdDependency(node1.Id);
                    cache.Insert(key1, nodeData1, dependencies1);

                    // create node2 and cache it with it own NodeIdDependency
                    var node2 = CreateTestRoot();
                    var nodeId2 = node2.Id;
                    var nodeData2 = node2.Data;
                    var key2 = "NodeData." + node2.VersionId;
                    var dependencies2 = new NodeIdDependency(node2.Id);
                    cache.Insert(key2, nodeData2, dependencies2);

                    // pre-check: nodes are in the cache
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#1: remove unknown node via NodeIdDependency
                    NodeIdDependency.FireChanged(Math.Max(nodeId1, nodeId2) + 42);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#2: remove node1 wia NodeIdDependency
                    NodeIdDependency.FireChanged(nodeId1);
                    Assert.IsFalse(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // add node1 again
                    dependencies1 = new NodeIdDependency(node1.Id);
                    cache.Insert(key1, nodeData1, dependencies1);

                    // TEST#3: remove node1 wia NodeIdDependency
                    NodeIdDependency.FireChanged(nodeId2);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsFalse(IsInCache(key2));
                });
        }
        [TestMethod]
        public void Cache_Legacy_NodeIdDependency_Shared()
        {
            Test((builder) => { builder.UseCacheProvider(new TestAspNetCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;
                    cache.Reset();

                    // create node1 and cache it with a shared NodeIdDependency
                    var node1 = CreateTestRoot();
                    var nodeId1 = node1.Id;
                    var nodeData1 = node1.Data;
                    var key1 = "NodeData." + node1.VersionId;
                    var dependencies1 = new NodeIdDependency(nodeId1);
                    cache.Insert(key1, nodeData1, dependencies1);

                    // create node2 and cache it with NodeIdDependency by node1
                    var node2 = CreateTestRoot();
                    var nodeId2 = node2.Id;
                    var nodeData2 = node2.Data;
                    var key2 = "NodeData." + node2.VersionId;
                    var dependencies2 = new NodeIdDependency(nodeId1); // nodeId1 is the right value!
                    cache.Insert(key2, nodeData2, dependencies2);

                    // pre-check: nodes are in the cache
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#1: remove unknown node wia NodeIdDependency
                    NodeIdDependency.FireChanged(Math.Max(nodeId1, nodeId2) + 42);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#2: remove nodes wia nodeId2 (not affected)
                    NodeIdDependency.FireChanged(nodeId2);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#3: remove node1 wia NodeIdDependency
                    NodeIdDependency.FireChanged(nodeId1);
                    Assert.IsFalse(IsInCache(key1));
                    Assert.IsFalse(IsInCache(key2));
                });
        }

        [TestMethod]
        public void Cache_Legacy_PathDependency()
        {
            Test((builder) => { builder.UseCacheProvider(new TestAspNetCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;

                    var root = CreateTestFolder(Repository.Root);
                    // create node1 and cache it with it own NodeIdDependency
                    var node1 = CreateTestFolder(root);
                    var node11 = CreateTestFolder(node1);
                    var node12 = CreateTestFolder(node1);
                    var node2 = CreateTestFolder(root);
                    var node21 = CreateTestFolder(node2);
                    var node22 = CreateTestFolder(node2);

                    cache.Reset();

                    var rootKey = InsertCacheWithPathDependency(cache, root);
                    var node1Key = InsertCacheWithPathDependency(cache, node1);
                    var node11Key = InsertCacheWithPathDependency(cache, node11);
                    var node12Key = InsertCacheWithPathDependency(cache, node12);
                    var node2Key = InsertCacheWithPathDependency(cache, node2);
                    var node21Key = InsertCacheWithPathDependency(cache, node21);
                    var node22Key = InsertCacheWithPathDependency(cache, node22);

                    // pre-check: all nodes are in the cache
                    Assert.IsTrue(IsInCache(rootKey));
                    Assert.IsTrue(IsInCache(node1Key));
                    Assert.IsTrue(IsInCache(node11Key));
                    Assert.IsTrue(IsInCache(node12Key));
                    Assert.IsTrue(IsInCache(node2Key));
                    Assert.IsTrue(IsInCache(node21Key));
                    Assert.IsTrue(IsInCache(node22Key));

                    // TEST: Remove the subree of the node1
                    PathDependency.FireChanged(node1.Path);

                    // check: only node1 subtree is removed
                    Assert.IsTrue(IsInCache(rootKey));
                    Assert.IsFalse(IsInCache(node1Key));
                    Assert.IsFalse(IsInCache(node11Key));
                    Assert.IsFalse(IsInCache(node12Key));
                    Assert.IsTrue(IsInCache(node2Key));
                    Assert.IsTrue(IsInCache(node21Key));
                    Assert.IsTrue(IsInCache(node22Key));

                });
        }
        [TestMethod]
        public void Cache_Legacy_TypeDependency()
        {
            Test((builder) => { builder.UseCacheProvider(new TestAspNetCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;

                    var root = CreateTestFolder(Repository.Root);
                    // create node1 and cache it with it own NodeIdDependency
                    var folder1 = CreateTestFolder(root, true);
                    var file11 = CreateTestFile(folder1);
                    var folder11 = CreateTestFolder(folder1);
                    var file111 = CreateTestFile(folder11);

                    cache.Reset();

                    var rootKey = InsertCacheWithTypeDependency(cache, root);
                    var folder1Key = InsertCacheWithTypeDependency(cache, folder1);
                    var file11Key = InsertCacheWithTypeDependency(cache, file11);
                    var folder11Key = InsertCacheWithTypeDependency(cache, folder11);
                    var file111Key = InsertCacheWithTypeDependency(cache, file111);

                    // pre-check: all nodes are in the cache
                    Assert.IsTrue(IsInCache(rootKey));
                    Assert.IsTrue(IsInCache(folder1Key));
                    Assert.IsTrue(IsInCache(file11Key));
                    Assert.IsTrue(IsInCache(folder11Key));
                    Assert.IsTrue(IsInCache(file111Key));

                    // TEST: Remove the folder type tree
                    var before = WhatIsInTheCache(cache);
                    foreach (var nodeType in NodeType.GetByName("Folder").GetAllTypes())
                        NodeTypeDependency.FireChanged(nodeType.Id);
                    var after = WhatIsInTheCache(cache);

                    // check: all folders are removed
                    Assert.IsFalse(IsInCache(rootKey));
                    Assert.IsFalse(IsInCache(folder1Key));
                    Assert.IsTrue(IsInCache(file11Key));
                    Assert.IsFalse(IsInCache(folder11Key));
                    Assert.IsTrue(IsInCache(file111Key));
                });
        }

        [TestMethod]
        public void Cache_Legacy_PortletDependency()
        {
            Test((builder) => { builder.UseCacheProvider(new TestAspNetCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;
                    cache.Reset();

                    var key1 = "Key1";
                    var value1 = "CachedValue1";
                    var dependencies1 = new PortletDependency(key1);
                    cache.Insert(key1, value1, dependencies1);

                    var key2 = "Key2";
                    var value2 = "CachedValue2";
                    var dependencies2 = new PortletDependency(key2);
                    cache.Insert(key2, value2, dependencies2);

                    // pre-check: nodes are in the cache
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST: remove unknown node wia NodeIdDependency
                    PortletDependency.FireChanged(key1);
                    Assert.IsFalse(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));
                });
        }

        /* ================================================================================= */

        [TestMethod]
        public void Cache_Builtin_DeleteParent_SubtreeRemoved()
        {
            Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                () =>
                {
                    // create structure
                    var root = Repository.Root;
                    var rootHead = NodeHead.Get(root.Id);
                    var testRoot = CreateTestRoot();
                    var testRootHead = NodeHead.Get(testRoot.Id);
                    var file = new File(testRoot) { Name = "file" };
                    file.Binary.SetStream(RepositoryTools.GetStreamFromString("Lorem ipsum..."));
                    file.Save();
                    var fileHead = NodeHead.Get(file.Id);
                    var binaryData = file.Binary;

                    // reset cache
                    DistributedApplication.Cache.Reset();

                    root = Node.Load<PortalRoot>(root.Id);
                    testRoot = Node.Load<SystemFolder>(testRoot.Id);
                    file = Node.Load<File>(file.Id);
                    var stream = file.Binary.GetStream();

                    Assert.IsTrue(IsInCache(root.Data));
                    Assert.IsTrue(IsInCache(rootHead));
                    Assert.IsTrue(IsInCache(testRoot.Data));
                    Assert.IsTrue(IsInCache(testRootHead));
                    Assert.IsTrue(IsInCache(file.Data));
                    Assert.IsTrue(IsInCache(fileHead));
                    Assert.IsTrue(IsInCache(binaryData));

                    //var keysBefore = cache.WhatIsInTheCache();

                    testRoot.ForceDelete();

                    Assert.IsTrue(IsInCache(root.Data));
                    Assert.IsTrue(IsInCache(rootHead));
                    Assert.IsFalse(IsInCache(testRoot.Data));
                    Assert.IsFalse(IsInCache(testRootHead));
                    Assert.IsFalse(IsInCache(file.Data));
                    Assert.IsFalse(IsInCache(fileHead));
                    Assert.IsFalse(IsInCache(binaryData));
                });
        }

        [TestMethod]
        public void Cache_Builtin_NodeIdDependency_Private()
        {
            Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;
                    cache.Reset();

                    // create node1 and cache it with it own NodeIdDependency
                    var node1 = CreateTestRoot();
                    var nodeId1 = node1.Id;
                    var nodeData1 = node1.Data;
                    var key1 = "NodeData." + node1.VersionId;
                    var dependencies1 = new NodeIdDependency(node1.Id);
                    cache.Insert(key1, nodeData1, dependencies1);

                    // create node2 and cache it with it own NodeIdDependency
                    var node2 = CreateTestRoot();
                    var nodeId2 = node2.Id;
                    var nodeData2 = node2.Data;
                    var key2 = "NodeData." + node2.VersionId;
                    var dependencies2 = new NodeIdDependency(node2.Id);
                    cache.Insert(key2, nodeData2, dependencies2);

                    // pre-check: nodes are in the cache
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#1: remove unknown node via NodeIdDependency
                    NodeIdDependency.FireChanged(Math.Max(nodeId1, nodeId2) + 42);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#2: remove node1 wia NodeIdDependency
                    NodeIdDependency.FireChanged(nodeId1);
                    Assert.IsFalse(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // add node1 again
                    dependencies1 = new NodeIdDependency(node1.Id);
                    cache.Insert(key1, nodeData1, dependencies1);

                    // TEST#3: remove node1 wia NodeIdDependency
                    NodeIdDependency.FireChanged(nodeId2);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsFalse(IsInCache(key2));
                });
        }
        [TestMethod]
        public void Cache_Builtin_NodeIdDependency_Shared()
        {
            Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;
                    cache.Reset();

                    // create node1 and cache it with a shared NodeIdDependency
                    var node1 = CreateTestRoot();
                    var nodeId1 = node1.Id;
                    var nodeData1 = node1.Data;
                    var key1 = "NodeData." + node1.VersionId;
                    var dependencies1 = new NodeIdDependency(nodeId1);
                    cache.Insert(key1, nodeData1, dependencies1);

                    // create node2 and cache it with NodeIdDependency by node1
                    var node2 = CreateTestRoot();
                    var nodeId2 = node2.Id;
                    var nodeData2 = node2.Data;
                    var key2 = "NodeData." + node2.VersionId;
                    var dependencies2 = new NodeIdDependency(nodeId1); // nodeId1 is the right value!
                    cache.Insert(key2, nodeData2, dependencies2);

                    // pre-check: nodes are in the cache
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#1: remove unknown node wia NodeIdDependency
                    NodeIdDependency.FireChanged(Math.Max(nodeId1, nodeId2) + 42);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#2: remove nodes wia nodeId2 (not affected)
                    NodeIdDependency.FireChanged(nodeId2);
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST#3: remove node1 wia NodeIdDependency
                    NodeIdDependency.FireChanged(nodeId1);
                    Assert.IsFalse(IsInCache(key1));
                    Assert.IsFalse(IsInCache(key2));
                });
        }

        [TestMethod]
        public void Cache_Builtin_PathDependency()
        {
            Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;

                    var root = CreateTestFolder(Repository.Root);
                    // create node1 and cache it with it own NodeIdDependency
                    var node1 = CreateTestFolder(root);
                    var node11 = CreateTestFolder(node1);
                    var node12 = CreateTestFolder(node1);
                    var node2 = CreateTestFolder(root);
                    var node21 = CreateTestFolder(node2);
                    var node22 = CreateTestFolder(node2);

                    cache.Reset();

                    var rootKey = InsertCacheWithPathDependency(cache, root);
                    var node1Key = InsertCacheWithPathDependency(cache, node1);
                    var node11Key = InsertCacheWithPathDependency(cache, node11);
                    var node12Key = InsertCacheWithPathDependency(cache, node12);
                    var node2Key = InsertCacheWithPathDependency(cache, node2);
                    var node21Key = InsertCacheWithPathDependency(cache, node21);
                    var node22Key = InsertCacheWithPathDependency(cache, node22);

                    // pre-check: all nodes are in the cache
                    Assert.IsTrue(IsInCache(rootKey));
                    Assert.IsTrue(IsInCache(node1Key));
                    Assert.IsTrue(IsInCache(node11Key));
                    Assert.IsTrue(IsInCache(node12Key));
                    Assert.IsTrue(IsInCache(node2Key));
                    Assert.IsTrue(IsInCache(node21Key));
                    Assert.IsTrue(IsInCache(node22Key));

                    // TEST: Remove the subree of the node1
                    PathDependency.FireChanged(node1.Path);

                    // check: only node1 subtree is removed
                    Assert.IsTrue(IsInCache(rootKey));
                    Assert.IsFalse(IsInCache(node1Key));
                    Assert.IsFalse(IsInCache(node11Key));
                    Assert.IsFalse(IsInCache(node12Key));
                    Assert.IsTrue(IsInCache(node2Key));
                    Assert.IsTrue(IsInCache(node21Key));
                    Assert.IsTrue(IsInCache(node22Key));

                });
        }
        [TestMethod]
        public void Cache_Builtin_TypeDependency()
        {
            Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;

                    var root = CreateTestFolder(Repository.Root);
                    // create node1 and cache it with it own NodeIdDependency
                    var folder1 = CreateTestFolder(root, true);
                    var file11 = CreateTestFile(folder1);
                    var folder11 = CreateTestFolder(folder1);
                    var file111 = CreateTestFile(folder11);

                    cache.Reset();

                    var rootKey = InsertCacheWithTypeDependency(cache, root);
                    var folder1Key = InsertCacheWithTypeDependency(cache, folder1);
                    var file11Key = InsertCacheWithTypeDependency(cache, file11);
                    var folder11Key = InsertCacheWithTypeDependency(cache, folder11);
                    var file111Key = InsertCacheWithTypeDependency(cache, file111);

                    // pre-check: all nodes are in the cache
                    Assert.IsTrue(IsInCache(rootKey));
                    Assert.IsTrue(IsInCache(folder1Key));
                    Assert.IsTrue(IsInCache(file11Key));
                    Assert.IsTrue(IsInCache(folder11Key));
                    Assert.IsTrue(IsInCache(file111Key));

                    // TEST: Remove the folder type tree
                    var before = WhatIsInTheCache(cache);
                    foreach (var nodeType in NodeType.GetByName("Folder").GetAllTypes())
                        NodeTypeDependency.FireChanged(nodeType.Id);
                    var after = WhatIsInTheCache(cache);

                    // check: all folders are removed
                    Assert.IsFalse(IsInCache(rootKey));
                    Assert.IsFalse(IsInCache(folder1Key));
                    Assert.IsTrue(IsInCache(file11Key));
                    Assert.IsFalse(IsInCache(folder11Key));
                    Assert.IsTrue(IsInCache(file111Key));
                });
        }

        [TestMethod]
        public void Cache_Builtin_PortletDependency()
        {
            Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;
                    cache.Reset();

                    var key1 = "Key1";
                    var value1 = "CachedValue1";
                    var dependencies1 = new PortletDependency(key1);
                    cache.Insert(key1, value1, dependencies1);

                    var key2 = "Key2";
                    var value2 = "CachedValue2";
                    var dependencies2 = new PortletDependency(key2);
                    cache.Insert(key2, value2, dependencies2);

                    // pre-check: nodes are in the cache
                    Assert.IsTrue(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));

                    // TEST: remove unknown node wia NodeIdDependency
                    PortletDependency.FireChanged(key1);
                    Assert.IsFalse(IsInCache(key1));
                    Assert.IsTrue(IsInCache(key2));
                });
        }


        [TestMethod]
        public void Cache_DependencyCounts()
        {
            Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                () =>
                {
                    var cache = DistributedApplication.Cache;
                    cache.Reset();

                    var idArray = CreateSafeContentQuery("InTree:/Root").Execute().Nodes
                        .Select(n => n.Id).ToArray();

                    var countBefore = cache.Count;
                    Assert.IsTrue(countBefore > idArray.Length * 2);
                    var eventCountsBefore = cache.Events.GetCounts();
                    var totalEventCountBefore = eventCountsBefore.Select(x => x.Value.Sum()).Sum();
                    Assert.IsTrue(totalEventCountBefore > countBefore * 2);

                    PathDependency.FireChanged("/Root/System");

                    var countAfter = cache.Count;
                    var eventCountsAfter = cache.Events.GetCounts();
                    var totalEventCountAfter = eventCountsAfter.Select(x => x.Value.Sum()).Sum();
                    Assert.IsTrue(0 < countAfter);
                    Assert.IsTrue(countAfter < countBefore);
                    Assert.IsTrue(totalEventCountAfter > countAfter * 2);
                    Assert.IsTrue(totalEventCountAfter < totalEventCountBefore);

                    PathDependency.FireChanged("/Root");

                    var countFinal = cache.Count;
                    var eventCountsFinal = cache.Events.GetCounts();
                    var totalEventCountFinal = eventCountsFinal.Select(x => x.Value.Sum()).Sum();
                    Assert.AreEqual(0, countFinal);
                    Assert.AreEqual(0, totalEventCountFinal);
                });
        }


        [TestMethod]
        public void Cache_Measuring()
        {
            var tracer = new SnDebugViewTracer();
            SnTrace.SnTracers.Add(tracer);
            try
            {
                var partitionConfig = new[] {25, 50, 100, 200, 400, 800};
                foreach (var partitions in partitionConfig)
                {
                    Cache.NodeIdDependencyEventPartitions = partitions;
                    Cache.NodeTypeDependencyEventPartitions = partitions;
                    Cache.PathDependencyEventPartitions = partitions;
                    Cache.PortletDependencyEventPartitions = partitions;
                    Test((builder) => { builder.UseCacheProvider(new SnMemoryCache()); },
                        () =>
                        {
                            if (SnMaintenance.Running())
                                new SnMaintenance().Shutdown();

                            var nodes = CreateSafeContentQuery("InTree:/Root").Execute().Nodes.ToArray();
                            var averages = new double[10];

                            for (var k = 1; k < 11; k++)
                            {
                                var times = new TimeSpan[12];

                                SnTrace.Write($"k = {k}");
                                for (var j = 0; j < 12; j++)
                                {
                                    var cache = DistributedApplication.Cache;
                                    cache.Reset();

                                    foreach (var node in nodes)
                                        for (var i = 0; i < 40 * k; i++)
                                            cache.Insert($"ExtraEntry.{node.Id}.{i}", node.Data, new PathDependency(node.Path));

                                    var countBefore = cache.Count;
                                    var eventCountsBefore = cache.Events.GetCounts();
                                    var totalEventCountBefore = eventCountsBefore.Select(x => x.Value.Sum()).Sum();

                                    times[j] = Watch(() => { PathDependency.FireChanged("/Root/System"); });

                                    var countAfter = cache.Count;
                                    var eventCountsAfter = cache.Events.GetCounts();
                                    var totalEventCountAfter = eventCountsAfter.Select(x => x.Value.Sum()).Sum();

                                    //SnTrace.Write($"{countBefore},{totalEventCountBefore} - {countAfter},{totalEventCountAfter}: {times[j]}");
                                }

                                var avg = times
                                    .Select(t => t.TotalSeconds)
                                    .OrderBy(x => x)
                                    .Skip(1)
                                    .Take(10)
                                    .Average(x => x);
                                averages[k - 1] = avg;
                            }

                            SnTrace.Write($"Average times ({partitions} partitions):");
                            foreach (var average in averages)
                                SnTrace.Write($"Average: {average}");
                        });
                }
            }
            finally
            {
                SnTrace.SnTracers.Remove(tracer);
            }
            Assert.Inconclusive();
        }

        private TimeSpan Watch(Action action)
        {
            var stopper = Stopwatch.StartNew();
            action();
            stopper.Stop();
            return stopper.Elapsed;
        }

        /* ================================================================================= */
        private string InsertCacheWithPathDependency(ISnCache cache, Node node)
        {
            var cacheKey = "NodeData." + node.VersionId;

            var dependencies = new PathDependency(node.Path);
            cache.Insert(cacheKey, node.Data, dependencies);
            return cacheKey;
        }
        private string InsertCacheWithTypeDependency(ISnCache cache, Node node)
        {
            var cacheKey = "NodeData." + node.VersionId;

            var dependencies = new NodeTypeDependency(node.NodeTypeId);
            cache.Insert(cacheKey, node.Data, dependencies);
            return cacheKey;
        }

        private Node CreateTestRoot()
        {
            return CreateTestFolder(Repository.Root);
        }

        private Node CreateTestFolder(Node parent, bool simpleFolder = false)
        {
            var node = simpleFolder ? new Folder(parent) : new SystemFolder(parent);
            node.Name = Guid.NewGuid().ToString();
            node.Save();
            return node;
        }
        private Node CreateTestFile(Node parent)
        {
            var file = new File(parent) { Name = Guid.NewGuid().ToString() };
            file.Binary.SetStream(RepositoryTools.GetStreamFromString(Guid.NewGuid().ToString()));
            file.Save();
            return file;
        }

        private bool IsInCache(object obj)
        {
            foreach (var key in GetKeys(obj))
            {
                if (!IsInCache(key))
                    return false;
            }
            return true;
        }
        private bool IsInCache(string key)
        {
            return DistributedApplication.Cache.Get(key) != null;
        }
        private string[] GetKeys(object obj)
        {
            if (obj is NodeHead nodeHead)
            {
                return new[] { $"NodeHeadCache.{nodeHead.Id}", $"NodeHeadCache.{nodeHead.Path.ToLowerInvariant()}" };
            }
            if (obj is NodeData nodeData)
            {
                return new[] { $"NodeData.{nodeData.VersionId}" };
            }
            if (obj is BinaryData binaryData)
            {
                return new[] {$"RawBinary.{binaryData.OwnerNode.VersionId}.{binaryData.PropertyType.Id}"};
            }
            throw new NotImplementedException($"Getting cache key for a {obj.GetType().Name} is not implemented.");
        }

        private string WhatIsInTheCache(ISnCache cache)
        {
            var sb = new StringBuilder();
            foreach (var x in cache)
                sb.AppendLine(x.Key);
            return sb.ToString();
        }

    }
}
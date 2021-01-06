﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Search.Indexing;

namespace SenseNet.IntegrationTests.Infrastructure
{
    [TestClass]
    public class Initializer
    {
        [AssemblyInitialize]
        public static void InitializeAllTests(TestContext testContext)
        {
            Logger.ClearLog();
            Logger.Log("All tests started.");
        }
        [AssemblyCleanup]
        public static void CleanupAllTests()
        {
            // Close the last repository if any.
            TestCaseBase.CleanupClass();
            Logger.Log("All tests finished.");
        }

        public static InitialData InitialData { get; } =
            InitialData.Load(InMemoryTestData.Instance, InMemoryTestIndexDocuments.IndexDocuments);

        private static InMemoryIndex _initialIndex;

        public static InMemoryIndex GetInitialIndex()
        {
            if (_initialIndex == null)
            {
                var index = new InMemoryIndex();
                index.Load(new StringReader(InMemoryTestIndex.Index));
                _initialIndex = index;
            }

            return _initialIndex.Clone();
        }

        public static IEnumerable<IndexDocument> GetInitialIndexDocuments()
        {
            var deserialized = InMemoryTestIndexDocuments.IndexDocuments
                .Select(IndexDocument.Deserialize)
                .ToArray();

            return deserialized;
        }

    }
}

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tests.Core;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.InMemory;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ExclusiveBlockTests : TestBase
    {
        private async STT.Task Worker(string operationId, ExclusiveBlockType blockType, TimeSpan timeout,
            List<string> log)
        {
            log.Add("before block " + operationId);
            await ExclusiveBlock.RunAsync("MyFeature", operationId, blockType, timeout, async () =>
            {
                await STT.Task.Delay(1500);
                log.Add("in block " + operationId);
            });
            log.Add("after block " + operationId);
        }

        [TestMethod]
        public void ExclusiveBlock_SkipIfLocked()
        {
            Initialize();
            var log = new List<string>();

            var task1 = Worker("1", ExclusiveBlockType.SkipIfLocked, TimeSpan.Zero, log);
            var task2 = Worker("2", ExclusiveBlockType.SkipIfLocked, TimeSpan.Zero, log);
            STT.Task.WaitAll(task1, task2);
            Thread.Sleep(100);

            // LOG EXAMPLE:
            // "before block 1"
            // "before block 2"
            // "after block 2"
            // "in block 1"
            // "after block 1"

            var inBlockCount = log.Count(x => x.StartsWith("in block"));
            Assert.AreEqual(1, inBlockCount);
        }

        [TestMethod]
        public void ExclusiveBlock_WaitForReleased()
        {
            Initialize();
            var log = new List<string>();

            var task1 = Worker("1", ExclusiveBlockType.WaitForReleased, TimeSpan.Zero, log);
            var task2 = Worker("2", ExclusiveBlockType.WaitForReleased, TimeSpan.Zero, log);
            STT.Task.WaitAll(task1, task2);
            Thread.Sleep(100);

            // LOG EXAMPLE:
            // "before block 1"
            // "before block 2"
            // "in block 1"
            // "after block 1"
            // "after block 2"

            var inBlockCount = log.Count(x => x.StartsWith("in block"));
            Assert.AreEqual(1, inBlockCount);
        }

        [TestMethod]
        public void ExclusiveBlock_WaitAndAcquire()
        {
            Initialize();
            var log = new List<string>();
SnTrace.Write("## -----------------------------------------------------------------------------");

            var task1 = Worker("1", ExclusiveBlockType.WaitAndAcquire, TimeSpan.FromSeconds(2), log);
            var task2 = Worker("2", ExclusiveBlockType.WaitAndAcquire, TimeSpan.FromSeconds(2), log);
            STT.Task.WaitAll(task1, task2);
            Thread.Sleep(100);

            // LOG EXAMPLE:
            // "before block 1"
            // "before block 2"
            // "in block 1"
            // "after block 1"
            // "in block 2"
            // "after block 2"

            var inBlockCount = log.Count(x => x.StartsWith("in block"));
            Assert.AreEqual(2, inBlockCount);
        }

        private void Initialize()
        {
            Providers.Instance.DataProvider = new InMemoryDataProvider();
            DataStore.DataProvider.SetExtension(typeof(IExclusiveLockDataProviderExtension),
                new InMemoryExclusiveLockDataProvider());
            SnTrace.Custom.Enabled = true;
            SnTrace.System.Enabled = true;
        }
    }
}
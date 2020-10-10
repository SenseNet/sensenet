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
        private async STT.Task Worker(string operationId, ExclusiveBlockType blockType,
            List<string> log, TimeSpan timeout = default)
        {
            var context = new ExclusiveBlockContext
            {
                OperationId = operationId,
                LockTimeout = TimeSpan.FromSeconds(1),
                PollingTime = TimeSpan.FromSeconds(0.1),
            };
            if (timeout != default)
                context.WaitTimeout = timeout;

            log.Add("before block " + operationId);
            await ExclusiveBlock.RunAsync(context, "MyFeature", blockType, async () =>
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

            var task1 = Worker("1", ExclusiveBlockType.SkipIfLocked, log);
            var task2 = Worker("2", ExclusiveBlockType.SkipIfLocked, log);
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

            var task1 = Worker("1", ExclusiveBlockType.WaitForReleased, log);
            var task2 = Worker("2", ExclusiveBlockType.WaitForReleased, log);
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

            var task1 = Worker("1", ExclusiveBlockType.WaitAndAcquire, log, TimeSpan.FromSeconds(2));
            var task2 = Worker("2", ExclusiveBlockType.WaitAndAcquire, log, TimeSpan.FromSeconds(2));
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
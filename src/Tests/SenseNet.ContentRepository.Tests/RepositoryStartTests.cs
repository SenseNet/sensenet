using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Security;
using SenseNet.Security.Messaging;
using SenseNet.Security.Messaging.SecurityMessages;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class RepositoryStartTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.Inconclusive("Repository start test not implemented.");

            //var repoBuilder = new RepositoryBuilder()
            //    .UseDataProvider(new TestDataProvider())
            //    .UseSecurityDataProvider(new TestSecurityDbProvider())
            //    .UseElevatedModificationVisibilityRuleProvider(new ElevatedModificationVisibilityRule())
            //    .StartLuceneManager(false)
            //    .BackupIndexAtTheEnd(false)
            //    .StartWorkflowEngine(false)
            //    .RestoreIndex(false);

            //using (var repo = Repository.Start(repoBuilder))
            //{

            //}
        }
    }
}

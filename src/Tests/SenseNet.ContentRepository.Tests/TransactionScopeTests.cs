//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using SenseNet.ContentRepository.Storage;
//using SenseNet.Tests;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace SenseNet.ContentRepository.Tests
//{
//    [TestClass]
//    public class TransactionScopeTests : TestBase
//    {
//        [TestMethod]
//        public void TransactionScope_IsActive_PassiveTest()
//        {
//            Test(() =>
//            {
//                Assert.AreEqual(false, TransactionScope.IsActive);
//            });
//        }

//        [TestMethod]
//        public void TransactionScope_IsolationLevel_PassiveTest()
//        {
//            Test(() =>
//            {
//                Assert.AreEqual(IsolationLevel.Unspecified, TransactionScope.IsolationLevel);
//            });
//        }

//        [TestMethod]
//        public void TransactionScope_IsActive()
//        {
//            Test(() =>
//            {
//                TransactionScope.Begin();
//                Assert.AreEqual(true, TransactionScope.IsActive);
//                TransactionScope.Rollback();
//            });
//        }

//        [TestMethod]
//        public void TransactionScope_IsolationLevel()
//        {
//            Test(() =>
//            {
//                TransactionScope.Begin();
//                Assert.AreEqual(IsolationLevel.ReadCommitted, TransactionScope.IsolationLevel);
//                TransactionScope.Rollback();
//            });
//        }

//        [TestMethod]
//        public void TransactionScope_IsolationLevel_ValueTest1()
//        {
//            Test(() =>
//            {
//                TransactionScope.Begin(IsolationLevel.Serializable);
//                Assert.AreEqual(IsolationLevel.Serializable, TransactionScope.IsolationLevel);
//                TransactionScope.Rollback();
//            });
//        }

//        [TestMethod]
//        public void TransactionScope_Commit_UseCase1()
//        {
//            Test(() =>
//            {
//                var name = "CommitTest-1";

//                TransactionScope.Begin();

//                var folder = new Folder(Repository.Root);
//                folder.Name = name;
//                folder.Save();

//                TransactionScope.Commit();

//                folder = (Folder)Node.LoadNode(folder.Id);
//                Assert.IsNotNull(folder);
//                Assert.AreEqual(name, folder.Name);
//            });
//        }

//        [TestMethod]
//        [ExpectedException(typeof(InvalidOperationException))]
//        public void TransactionScope_Begin_AlreadyActive()
//        {
//            Test(() =>
//            {
//                try
//                {
//                    TransactionScope.Begin();
//                    TransactionScope.Begin();
//                }
//                finally
//                {
//                    TransactionScope.Rollback();
//                }
//            });
//        }

//        [TestMethod]
//        [ExpectedException(typeof(InvalidOperationException))]
//        public void TransactionScope_Commit_NonActive()
//        {
//            Test(() =>
//            {
//                TransactionScope.Commit();
//            });
//        }

//        [TestMethod]
//        [ExpectedException(typeof(InvalidOperationException))]
//        public void TransactionScope_Rollback_NonActive()
//        {
//            Test(() =>
//            {
//                TransactionScope.Rollback();
//            });
//        }

//        //[TestMethod]
//        public void TransactionScope_Rollback_UseCase1()
//        {
//            Test(() =>
//            {
//                var name = "RollbackTest-1";

//                var folder = new Folder(Repository.Root);
//                folder.Name = name;
//                TransactionScope.Begin();

//                folder.Save();
//                int id = folder.Id;
//                Assert.AreNotEqual(0, id);

//                TransactionScope.Rollback();

//                Assert.AreEqual(0, folder.Id, "Node.Id must be set back to 0 after a rollback event.");
//                folder = (Folder)Node.LoadNode(id);
//                Assert.IsNull(folder);
//            });
//        }
//    }
//}

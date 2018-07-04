using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public class UnitTest1
    {
        private static string _connectionString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=sn7tests;Data Source=.\SQL2016";

        private enum TestMode{BuiltIn, BuiltInFs, Legacy, LegacyFs};
        private static readonly Dictionary<TestMode, string> ConnectionStrings = new Dictionary<TestMode, string>
        {
            {TestMode.BuiltIn, @"Initial Catalog=sn7tests;Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False"},
            {TestMode.BuiltInFs, ""},
            {TestMode.Legacy, ""},
            {TestMode.LegacyFs, ""},
        };

        [TestMethod]
        public void Blob_BuiltIn_()
        {
        }
        [TestMethod]
        public void Blob_BuiltInFS_()
        {
        }
        [TestMethod]
        public void Blob_Legacy_()
        {
        }
        [TestMethod]
        public void Blob_LegacyFS_()
        {
        }
    }
}

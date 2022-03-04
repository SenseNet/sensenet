﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;
using SenseNet.IntegrationTests.TestCases;

namespace SenseNet.IntegrationTests.InMemTests
{
    [TestClass]
    public class InMemSearchTests : IntegrationTest<InMemPlatform, SearchTestCases>
    {
        [TestMethod]
        public void IntT_InMem_Search_ReferenceField()
        {
            TestCase.Search_ReferenceField();
        }
    }
}

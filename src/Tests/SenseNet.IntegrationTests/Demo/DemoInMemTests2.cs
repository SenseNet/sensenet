﻿using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.IntegrationTests.Infrastructure;
using SenseNet.IntegrationTests.Platforms;

namespace SenseNet.IntegrationTests.Demo
{
    [TestClass]
    public class DemoInMemTests2 : IntegrationTest<InMemPlatform, DemoTestCase2>
    {
        [TestMethod] public void IntegrationDemo_InMem_2_1() { TestCase.TestCase_2_1(); }
        [TestMethod] public void IntegrationDemo_InMem_2_2u() { TestCase.TestCase_2_2u(); }
        [TestMethod] public void IntegrationDemo_InMem_2_3() { TestCase.TestCase_2_3(); }
        [TestMethod] public void IntegrationDemo_InMem_2_4i() { TestCase.TestCase_2_4i(); }
        [TestMethod] public void IntegrationDemo_InMem_2_5() { TestCase.TestCase_2_5(); }
        [TestMethod] public async Task IntegrationDemo_InMem_2_6a() { await TestCase.TestCase_2_6a(); }
        [TestMethod] public void IntegrationDemo_InMem_2_7() { TestCase.TestCase_2_7(); }
        [TestMethod] public void IntegrationDemo_InMem_2_8() { TestCase.TestCase_2_8(); }
    }
}

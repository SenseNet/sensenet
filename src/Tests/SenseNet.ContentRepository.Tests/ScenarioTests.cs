using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.Tests;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ScenarioTests : TestBase
    {
        [Scenario("TestScenario1", true)] private class TestScenario1 : GenericScenario { }
        [Scenario("TestScenario2", false)] private class TestScenario2 : GenericScenario { }

        [TestMethod]
        public void AppModel_Scenario_ResolveSingleton()
        {
            Test(() =>
            {
                var instance1 = ScenarioManager.GetScenario("TestScenario1");
                var instance2 = ScenarioManager.GetScenario("TestScenario1");
                Assert.AreSame(instance1, instance2);
            });
        }
        [TestMethod]
        public void AppModel_Scenario_ResolveNotSingleton()
        {
            Test(() =>
            {
                var instance1 = ScenarioManager.GetScenario("TestScenario2");
                var instance2 = ScenarioManager.GetScenario("TestScenario2");
                Assert.AreNotSame(instance1, instance2);
            });
        }
        [TestMethod]
        public void AppModel_Scenario_GenericIsSingleton()
        {
            Test(() =>
            {
                // Get first generic scenario
                var genericScenario = ApplicationStorage.Instance.ScenarioNames
                    .Select(ScenarioManager.GetScenario)
                    .Where(s => s != null)
                    .FirstOrDefault(s => s.GetType() == typeof(GenericScenario));

                // The test method is invalid if there is no any generic scenario
                if (genericScenario == null)
                    Assert.Inconclusive();

                // Test
                var instance1 = ScenarioManager.GetScenario(genericScenario.Name);
                var instance2 = ScenarioManager.GetScenario(genericScenario.Name);
                Assert.AreSame(instance1, instance2);
            });
        }
    }
}

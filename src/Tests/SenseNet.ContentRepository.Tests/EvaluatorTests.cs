using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Scripting;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    [ScriptTagName("testTag")]
    internal class TestEvaluator : IEvaluator
    {
        public string Evaluate(string source)
        {
            return "success";
        }
    }
    [ScriptTagName("testTag")]
    internal class CustomTestEvaluator : IEvaluator
    {
        public string Evaluate(string source)
        {
            return "custom";
        }
    }

    [TestClass]
    public class EvaluatorTests : TestBase
    {
        [TestMethod]
        public void Evaluator_Eval_Existing()
        {
            const string script = "[Script:testTag]...myscript...[/Script]";

            Assert.AreEqual("success", Evaluator.Evaluate(script));
        }
        [TestMethod]
        public void Evaluator_Eval_NonExisting()
        {
            const string script = "[Script:wrongTag]...myscript...[/Script]";

            Assert.AreEqual("...myscript...", Evaluator.Evaluate(script));
        }
        [TestMethod]
        public void Evaluator_Eval_Custom()
        {
            const string script = "[Script:testTag]...myscript...[/Script]";

            var original = Providers.Instance.GetProvider<IEvaluator>(Evaluator.GetFullTagName("testTag"));
            var repo = new RepositoryBuilder();
            repo.UseScriptEvaluator(new CustomTestEvaluator());

            // the custom evaluator should be in control
            Assert.AreEqual("custom", Evaluator.Evaluate(script));

            // back to the original
            repo.UseScriptEvaluator(original);
            
            Assert.AreEqual("success", Evaluator.Evaluate(script));
        }

        [TestMethod]
        public void Evaluate_DefaultValue()
        {
            Test(() =>
            {
                Assert.AreEqual(null, FieldSetting.EvaluateDefaultValue(null));
                Assert.AreEqual(string.Empty, FieldSetting.EvaluateDefaultValue(""));
                Assert.AreEqual("@currentdate@", FieldSetting.EvaluateDefaultValue("@currentdate@")); // not a template syntax
                Assert.AreEqual("@@NOTEMPLATE@@", FieldSetting.EvaluateDefaultValue("@@NOTEMPLATE@@")); // unknown template

                using (new CurrentUserBlock(User.Administrator))
                {
                    Assert.AreEqual("1", FieldSetting.EvaluateDefaultValue("@@currentuser@@"));
                }

                var date1 = FieldSetting.EvaluateDefaultValue("@@currenttime@@");

                // the two dates can be a little bit different, because the main algorithm rounds the date to seconds
                Assert.IsTrue( DateTime.UtcNow - DateTime.Parse(date1) < TimeSpan.FromSeconds(1));
            });
        }
    }
}

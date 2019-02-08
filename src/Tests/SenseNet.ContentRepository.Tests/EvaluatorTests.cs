using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Scripting;
using SenseNet.Tests;

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
    }
}

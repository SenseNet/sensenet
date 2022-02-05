using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps
{
    internal class DisableIndexing : Step
    {
        public override void Execute(ExecutionContext context)
        {
            Providers.Instance.SearchManager.IsOuterEngineEnabled = false;
        }
    }
}

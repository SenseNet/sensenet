using SenseNet.ContentRepository.Search;

namespace SenseNet.Packaging.Steps
{
    internal class DisableIndexing : Step
    {
        public override void Execute(ExecutionContext context)
        {
            SearchManager.DisableOuterEngine();
        }
    }
}

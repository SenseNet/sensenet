using SenseNet.ContentRepository.Search;

namespace SenseNet.Packaging.Steps
{
    internal class EnableIndexing : Step
    {
        public override void Execute(ExecutionContext context)
        {
            SearchManager.EnableOuterEngine();
        }
    }
}

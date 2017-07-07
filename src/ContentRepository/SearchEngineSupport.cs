using System.Linq;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository
{
    internal class SearchEngineSupport : ISearchEngineSupport
    {
        public bool RestoreIndexOnstartup()
        {
            return RepositoryInstance.RestoreIndexOnStartup();
        }

        public int[] GetNotIndexedNodeTypeIds()
        {
            return new AllContentTypes()
                .Where(c => !c.IndexingEnabled)
                .Select(c => Storage.Schema.NodeType.GetByName(c.Name).Id)
                .ToArray();
        }

    }
}

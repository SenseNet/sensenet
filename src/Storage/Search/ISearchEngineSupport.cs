using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Search
{
    public interface ISearchEngineSupport //UNDONE:! Set an instance at system start
    {
        bool RestoreIndexOnstartup();
        int[] GetNotIndexedNodeTypeIds();
    }
}

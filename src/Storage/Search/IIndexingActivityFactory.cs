using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    public interface IIndexingActivityFactory
    {
        IIndexingActivity CreateActivity(IndexingActivityType activityType);
    }
}

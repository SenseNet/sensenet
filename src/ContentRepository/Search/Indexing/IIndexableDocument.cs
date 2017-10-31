using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    public interface IIndexableDocument
    {
        IEnumerable<IIndexableField> GetIndexableFields();
    }
}

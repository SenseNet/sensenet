using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.Search.Indexing
{
    [Serializable]
    [DebuggerDisplay("{Name}:{Type}={Value} | Store:{Store} Index:{Index} TermVector:{TermVector}")]
    public class IndexFieldInfo : IIndexFieldInfo
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public FieldInfoType Type { get; private set; }
        public IndexingMode Index { get; private set; }
        public IndexStoringMode Store { get; private set; }
        public IndexTermVector TermVector { get; private set; }

        public IndexFieldInfo(string name, string value, FieldInfoType type, IndexStoringMode store, IndexingMode index, IndexTermVector termVector)
        {
            Name = name;
            Value = value;
            Type = type;
            Store = store;
            Index = index;
            TermVector = termVector;
        }
    }
}

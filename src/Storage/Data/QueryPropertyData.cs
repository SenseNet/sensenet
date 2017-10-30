using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class QueryPropertyData
    {
        public string PropertyName { get; set; }
        public object Value { get; set; }

        public Operator QueryOperator { get; set; } = Operator.Equal;
    }
}

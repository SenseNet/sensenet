using SenseNet.ContentRepository.Storage.Search.Internal;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class QueryPropertyData
    {
        public string PropertyName { get; set; }
        public object Value { get; set; }

        private Operator _queryOperator = Operator.Equal;
        public Operator QueryOperator
        {
            get { return _queryOperator; }
            set { _queryOperator = value; }
        }
    }
}

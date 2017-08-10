using Lucene.Net.Documents;

namespace SenseNet.Search.Lucene29
{
    public class Lucene29IndexDocument : IIndexDocument
    {
        private readonly Document _document;

        public Lucene29IndexDocument(Document document)
        {
            _document = document;
        }

        public string Get(string fieldName)
        {
            return _document.Get(fieldName);
        }
    }
}

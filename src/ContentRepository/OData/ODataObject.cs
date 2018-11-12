namespace SenseNet.ContentRepository.OData
{
    public class ODataObject
    {
        public object Data { get; private set; }

        private ODataObject() { }

        public static ODataObject Create(object data)
        {
            return new ODataObject
            {
                Data = data
            };
        }
    }
}

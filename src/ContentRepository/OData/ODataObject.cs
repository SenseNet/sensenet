namespace SenseNet.ContentRepository.OData
{
    /// <summary>
    /// Wrapper class for custom objects you want to serve in an OData collection.
    /// Use the Create method to wrap individual objects in your list and return that
    /// as the result of an OData operation for the system to recognize it.
    /// </summary>
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

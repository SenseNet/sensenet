namespace SenseNet.ContentRepository.OData
{
    public class ODataCustomContent
    {
        public object Data { get; private set; }

        private ODataCustomContent() { }

        public static ODataCustomContent Create(object data)
        {
            return new ODataCustomContent
            {
                Data = data
            };
        }
    }
}

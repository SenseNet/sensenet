namespace SenseNet.OData.Responses
{
    public class ODataCollectionCountResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.CollectionCount;

        public int Count { get; set; }
        public override object Value => Count;

        public ODataCollectionCountResponse(int value)
        {
            Count = value;
        }
    }
}

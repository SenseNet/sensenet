namespace SenseNet.OData.Responses
{
    public class ODataCollectionCountResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.CollectionCount;

        public int Value { get; set; }
        public override object GetValue() => Value;

        public ODataCollectionCountResponse(int value)
        {
            Value = value;
        }
    }
}

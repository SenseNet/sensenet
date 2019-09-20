namespace SenseNet.OData.Responses
{
    public class ODataSingleContentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.SingleContent;

        public ODataContent Value { get; }
        public override object GetValue() => Value;

        public ODataSingleContentResponse(ODataContent value)
        {
            Value = value;
        }
    }
}

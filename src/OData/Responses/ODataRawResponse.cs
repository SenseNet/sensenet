namespace SenseNet.OData.Responses
{
    public class ODataRawResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.RawData;

        public object Value { get; set; }
        public override object GetValue() => Value;

        public ODataRawResponse(object value)
        {
            Value = value;
        }
    }
}

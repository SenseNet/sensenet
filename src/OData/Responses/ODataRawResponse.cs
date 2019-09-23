namespace SenseNet.OData.Responses
{
    public class ODataRawResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.RawData;

        public object RawValue { get; set; }
        public override object Value => RawValue;

        public ODataRawResponse(object value)
        {
            RawValue = value;
        }
    }
}

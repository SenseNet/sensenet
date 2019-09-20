namespace SenseNet.OData.Responses
{
    public class ODataErrorResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.Error;

        public ODataException Value { get; set; }
        public override object GetValue() => Value;

        public ODataErrorResponse(ODataException value)
        {
            Value = value;
        }
    }
}

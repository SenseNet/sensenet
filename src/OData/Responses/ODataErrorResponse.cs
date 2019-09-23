namespace SenseNet.OData.Responses
{
    public class ODataErrorResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.Error;

        public ODataException Exception { get; set; }
        public override object Value => Exception;

        public ODataErrorResponse(ODataException value)
        {
            Exception = value;
        }
    }
}

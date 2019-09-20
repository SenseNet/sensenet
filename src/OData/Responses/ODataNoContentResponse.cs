namespace SenseNet.OData.Responses
{
    public class ODataNoContentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.NoContent;
        public override object GetValue() => null;
    }
}

namespace SenseNet.OData.Responses
{
    public class ODataContentNotFoundResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ContentNotFound;
        public override object GetValue() => null;
    }
}

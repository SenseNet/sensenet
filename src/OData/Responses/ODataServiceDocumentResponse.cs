namespace SenseNet.OData.Responses
{
    public class ODataServiceDocumentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ServiceDocument;

        public string[] Value { get; set; }
        public override object GetValue() => Value;

        public ODataServiceDocumentResponse(string[] value)
        {
            Value = value;
        }
    }
}

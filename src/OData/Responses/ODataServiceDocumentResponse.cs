namespace SenseNet.OData.Responses
{
    public class ODataServiceDocumentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ServiceDocument;

        public string[] RootNames { get; set; }
        public override object Value => RootNames;

        public ODataServiceDocumentResponse(string[] value)
        {
            RootNames = value;
        }
    }
}

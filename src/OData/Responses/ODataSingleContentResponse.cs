namespace SenseNet.OData.Responses
{
    public class ODataSingleContentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.SingleContent;

        public ODataContent Entity { get; }
        public override object Value => Entity;

        public ODataSingleContentResponse(ODataContent value)
        {
            Entity = value;
        }
    }
}

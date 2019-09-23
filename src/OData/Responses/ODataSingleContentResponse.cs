namespace SenseNet.OData.Responses
{
    public class ODataSingleContentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.SingleContent;

        public ODataEntity Entity { get; }
        public override object Value => Entity;

        public ODataSingleContentResponse(ODataEntity value)
        {
            Entity = value;
        }
    }
}

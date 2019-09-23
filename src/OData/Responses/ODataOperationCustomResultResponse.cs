namespace SenseNet.OData.Responses
{
    public class ODataOperationCustomResultResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.OperationCustomResult;

        public int? AllCount { get; set; }
        public object ResultValue { get; set; }
        public override object Value => ResultValue;

        public ODataOperationCustomResultResponse(object value, int? allCount)
        {
            ResultValue = value;
            AllCount = allCount;
        }
    }
}

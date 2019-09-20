namespace SenseNet.OData.Responses
{
    public class ODataOperationCustomResultResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.OperationCustomResult;

        public int? AllCount { get; set; }
        public object Value { get; set; }
        public override object GetValue() => Value;

        public ODataOperationCustomResultResponse(object value, int? allCount)
        {
            Value = value;
            AllCount = allCount;
        }
    }
}

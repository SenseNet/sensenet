namespace SenseNet.OData.Responses
{
    public class ODataActionsPropertyRawResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ActionsPropertyRaw;

        public ODataActionItem[] Value { get; set; }
        public override object GetValue() => Value;

        public ODataActionsPropertyRawResponse(ODataActionItem[] value)
        {
            Value = value;
        }
    }
}

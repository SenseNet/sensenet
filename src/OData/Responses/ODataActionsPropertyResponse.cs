namespace SenseNet.OData.Responses
{
    public class ODataActionsPropertyResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ActionsProperty;

        public ODataActionItem[] Value { get; set; }
        public override object GetValue() => Value;

        public ODataActionsPropertyResponse(ODataActionItem[] value)
        {
            Value = value;
        }
    }
}

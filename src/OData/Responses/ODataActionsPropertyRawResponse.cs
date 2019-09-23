namespace SenseNet.OData.Responses
{
    public class ODataActionsPropertyRawResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ActionsPropertyRaw;

        public ODataActionItem[] Actions { get; set; }
        public override object Value => Actions;

        public ODataActionsPropertyRawResponse(ODataActionItem[] value)
        {
            Actions = value;
        }
    }
}

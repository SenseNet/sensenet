namespace SenseNet.OData.Responses
{
    public class ODataActionsPropertyResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ActionsProperty;

        public ODataActionItem[] Actions { get; set; }
        public override object Value => Actions;

        public ODataActionsPropertyResponse(ODataActionItem[] value)
        {
            Actions = value;
        }
    }
}

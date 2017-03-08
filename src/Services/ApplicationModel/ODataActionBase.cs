using SenseNet.Portal.OData;

namespace SenseNet.ApplicationModel
{
    public class ODataActionBase : ServiceAction
    {
        public override string ServiceName
        {
            get { return ODataTools.GetODataUrl(Content.Path).TrimStart('/'); }
            set { base.ServiceName = value; } 
        }

        public override bool IsODataOperation { get; } = true;
    }
}

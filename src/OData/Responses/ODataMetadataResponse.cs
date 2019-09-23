using System.Collections.Generic;

namespace SenseNet.OData.Responses
{
    public class ODataMetadataResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.MultipleContent;

        public string EntityPath { get; set; }
        public override object Value => EntityPath;

        public ODataMetadataResponse(string entityPath)
        {
            EntityPath = entityPath;
        }
    }
}

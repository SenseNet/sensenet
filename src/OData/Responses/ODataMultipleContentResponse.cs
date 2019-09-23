using System.Collections.Generic;

namespace SenseNet.OData.Responses
{
    public class ODataMultipleContentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.MultipleContent;

        public int AllCount { get; set; }
        public IEnumerable<ODataEntity> Entities { get; set; }
        public override object Value => Entities;

        public ODataMultipleContentResponse(IEnumerable<ODataEntity> value, int allCount)
        {
            Entities = value;
            AllCount = allCount;
        }
    }
}

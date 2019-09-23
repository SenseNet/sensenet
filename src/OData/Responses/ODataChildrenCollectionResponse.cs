using System.Collections.Generic;

namespace SenseNet.OData.Responses
{
    public class ODataChildrenCollectionResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.ChildrenCollection;

        public int AllCount { get; set; }
        public IEnumerable<ODataEntity> Entities { get; set; }
        public override object Value => Entities;

        public ODataChildrenCollectionResponse(IEnumerable<ODataEntity> value, int allCount)
        {
            Entities = value;
            AllCount = allCount;
        }
    }
}

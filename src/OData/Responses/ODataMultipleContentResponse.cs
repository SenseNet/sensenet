using System.Collections.Generic;

namespace SenseNet.OData.Responses
{
    public class ODataMultipleContentResponse : ODataResponse
    {
        public override ODataResponseType Type => ODataResponseType.MultipleContent;

        public int AllCount { get; set; }
        public IEnumerable<ODataContent> Value { get; set; }
        public override object GetValue() => Value;

        public ODataMultipleContentResponse(IEnumerable<ODataContent> value, int allCount)
        {
            Value = value;
            AllCount = allCount;
        }
    }
}

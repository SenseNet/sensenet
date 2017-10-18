using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SenseNet.Portal.OData
{
    internal class BatchActionResponse
    {
        [JsonProperty(PropertyName = "d", Order = 1)]
        public Dictionary<string, object> Contents { get; private set; }
        public static BatchActionResponse Create(IEnumerable<object> results, IEnumerable<ErrorContent> errors, int count = 0)
        {
            var resultArray = results.ToArray();
            var errorArray = errors.ToArray();
            var dict = new Dictionary<string, object>
            {
                {"__count", count == 0 ? resultArray.Length : count}
                ,{"results", resultArray}
                ,{"errors", errorArray}
            };
            return new BatchActionResponse { Contents = dict };
        }
    }
}
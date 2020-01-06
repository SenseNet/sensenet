using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ODataTests.Responses
{
    public class ODataEntitiesResponse : IEnumerable<ODataEntityResponse>, IODataResponse
    {
        private List<ODataEntityResponse> _entities;
        public int TotalCount { get; private set; }
        public int Length { get { return _entities.Count; } }

        public ODataEntitiesResponse(List<ODataEntityResponse> entities, int count)
        {
            _entities = entities;
            this.TotalCount = count;
        }

        public ODataEntityResponse this[int index]
        {
            get { return _entities[index]; }
        }

        public IEnumerator<ODataEntityResponse> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Services.OData.Tests.Results
{
    public class ODataEntities : IEnumerable<ODataEntity>, IODataResult
    {
        private List<ODataEntity> _entities;
        public int TotalCount { get; private set; }
        public int Length { get { return _entities.Count; } }

        public ODataEntities(List<ODataEntity> entities, int count)
        {
            _entities = entities;
            this.TotalCount = count;
        }

        public ODataEntity this[int index]
        {
            get { return _entities[index]; }
        }

        public IEnumerator<ODataEntity> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    [DebuggerDisplay("{ToString()}")]
    public class SortInfo
    {
        //UNDONE: let FieldName mandatory by a parametered constructor
        public string FieldName { get; set; }
        public bool Reverse { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", FieldName, Reverse ? "DESC" : "ASC");
        }
    }
}

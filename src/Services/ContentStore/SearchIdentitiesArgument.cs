using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Services.ContentStore
{
    public class SearchIdentitiesArgument
    {
        public bool SearchPersons { get; set; }
        public bool SearchGroups { get; set; }
        public bool SearchOrgUnits { get; set; }
        public bool SearchAD { get; set; }
        public string SearchString { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Portal.OData.Metadata
{
    public class EnumOption : NamedItem
    {
        public string Value;

        public override void WriteXml(System.IO.TextWriter writer)
        {
            // Do nothing: EnumType writes the XML.
        }
    }
}

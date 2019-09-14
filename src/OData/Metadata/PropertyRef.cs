using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.OData.Metadata
{
    public class PropertyRef : NamedItem
    {
        public override void WriteXml(TextWriter writer)
        {
            writer.Write("<PropertyRef Name=\"");
            writer.Write(Name);
            writer.Write("\"/>");
        }
    }
}

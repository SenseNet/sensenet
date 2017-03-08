using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

namespace SenseNet.Portal.OData.Metadata
{
    public abstract class SchemaItem
    {
        public abstract void WriteXml(TextWriter writer);

        // Writing xml helpers

        protected void WriteAttribute(TextWriter writer, string name, string value)
        {
            writer.Write(" ");
            writer.Write(name);
            writer.Write("=\"");
            writer.Write(value);
            writer.Write("\"");
        }
        protected void WriteCollectionXml(TextWriter writer, IEnumerable<SchemaItem> items)
        {
            if (items == null)
                return;
            foreach (var item in items)
                item.WriteXml(writer);
        }
    }
}

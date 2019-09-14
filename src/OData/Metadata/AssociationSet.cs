using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.OData.Metadata
{
    public class AssociationSet : NamedItem
    {
        public string Association; // "Self.ProductCategory"
        public AssociationSetEnd End1;
        public AssociationSetEnd End2;

        public override void WriteXml(TextWriter writer)
        {
            //        <AssociationSet Name="Products_Supplier_Suppliers"Association="ODataDemo.Product_Supplier_Supplier_Products">
            writer.Write("        <AssociationSet");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "Association", Association);
            writer.WriteLine(">");

            End1.WriteXml(writer);
            End2.WriteXml(writer);

            writer.WriteLine("        </AssociationSet>");
        }
    }
}

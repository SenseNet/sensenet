// ReSharper disable StringLiteralTypo
namespace SenseNet.OData.Metadata.Model
{
    public class DataServices : SchemaItem
    {
        public string DataServiceVersion = "3.0";
        public Schema[] Schemas;

        public override void WriteXml(System.IO.TextWriter writer)
        {
            writer.Write(@"  <edmx:DataServices xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" m:DataServiceVersion=""");
            writer.Write(MetaGenerator.dataServiceVersion);
            writer.WriteLine(@""">");

            foreach (var schema in Schemas)
                schema.WriteXml(writer);

            writer.WriteLine("  </edmx:DataServices>");
        }
    }
}

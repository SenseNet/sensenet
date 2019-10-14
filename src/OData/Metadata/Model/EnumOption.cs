namespace SenseNet.OData.Metadata.Model
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

﻿using System.IO;

namespace SenseNet.OData.Metadata.Model
{
    public class NavigationProperty : NamedItem
    {
        public string Relationship;
        public string FromRole;
        public string ToRole;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("        <NavigationProperty");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "Relationship", Relationship);
            WriteAttribute(writer, "FromRole", FromRole);
            WriteAttribute(writer, "ToRole", ToRole);
            writer.WriteLine("/>");
        }
    }
}

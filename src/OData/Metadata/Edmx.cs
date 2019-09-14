using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml;

namespace SenseNet.OData.Metadata
{
    public class Edmx : SchemaItem
    {
        public string Version = "1.0";
        public DataServices DataServices;

        public override void WriteXml(TextWriter writer)
        {
            writer.WriteLine(@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>");
            writer.Write(@"<edmx:Edmx Version=""");
            writer.Write(Version);
            writer.WriteLine(@""" xmlns:edmx=""http://schemas.microsoft.com/ado/2007/06/edmx"">");

            DataServices.WriteXml(writer);

            writer.WriteLine("</edmx:Edmx>");
        }

        public void WriteJson(TextWriter writer)
        {
            var serializer = Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            serializer.Serialize(writer, this);
        }

    }
}

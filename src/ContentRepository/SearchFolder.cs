using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using System.Linq;
using System.IO;
using SenseNet.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using SenseNet.ContentRepository.Fields;
using System.Xml.XPath;
using System.Web.Configuration;
using SenseNet.ApplicationModel;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    public class SearchFolder : FeedContent
    {
        public IEnumerable<Node> Children { get; private set; }

        private SearchFolder() { }

        public static SearchFolder Create(ContentQuery query)
        {
            var folder = new SearchFolder
            {
                Children = query.Execute().Nodes.ToArray()
            };
            return folder;
        }

        public static SearchFolder Create(IEnumerable<Node> nodes)
        {
            return new SearchFolder { Children = nodes };
        }

        protected override void WriteXml(XmlWriter writer, bool withChildren, SerializationOptions options)
        {
            const string thisName = "SearchFolder";
            const string thisPath = "/Root/SearchFolder";

            writer.WriteStartElement("Content");
            base.WriteHead(writer, thisName, thisName, thisName, thisPath, true);

            if (withChildren && Children != null)
            {
                writer.WriteStartElement("Children");
                this.WriteXml(Children, writer, options);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        protected override void WriteXml(XmlWriter writer, string referenceMemberName, SerializationOptions options)
        {
            WriteXml(writer, false, options);
        }
    }

}

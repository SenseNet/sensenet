using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Fields
{
	[ShortName("Reference")]
	[DataSlot(0, RepositoryDataType.Reference, typeof(IEnumerable), typeof(Node))]
	[DefaultFieldSetting(typeof(ReferenceFieldSetting))]
	[DefaultFieldControl("SenseNet.Portal.UI.Controls.ReferenceGrid")]
    [FieldDataType(typeof(IEnumerable<Node>))]
	public class ReferenceField : Field, SenseNet.ContentRepository.Xpath.IXmlChildList
    {
		protected override bool HasExportData
		{
			get
			{
                var data = GetData();
                if ((data as Node) != null || (data as IEnumerable) != null)
					return true;
                return false;
			}
		}

        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
		{
            var wm = RepositoryEnvironment.WorkingMode;
            if (wm.Exporting)
                ExportDataPath(writer, context);
            else
                ExportDataId(writer);
		}
        private void ExportDataPath(System.Xml.XmlWriter writer, ExportContext context)
        {
            var data = GetData();
            var node = data as Node;
            if (node != null)
            {
                writer.WriteStartElement("Path");
                writer.WriteString(node.Path);
                if (context != null)
                    context.AddReference(node.Path);
                writer.WriteEndElement();
                return;
            }

            var nodes = data as IEnumerable;
            if (nodes != null)
            {
                foreach (Node item in nodes)
                {
                    writer.WriteStartElement("Path");
                    writer.WriteString(item.Path);
                    if (context != null)
                        context.AddReference(item.Path);
                    writer.WriteEndElement();
                }
                return;
            }

            throw ExportNotSupportedException(GetData());
        }
        private void ExportDataId(System.Xml.XmlWriter writer)
        {
            var data = GetData();
            var node = data as Node;
            if (node != null)
            {
                writer.WriteString(node.Id.ToString());
                return;
            }

            var nodeList = data as NodeList<Node>;
            if (nodeList != null)
            {
                writer.WriteString(String.Join(",", nodeList.GetIdentifiers()));
                return;
            }

            var nodeEnumerable = data as IEnumerable<Node>;
            if (nodeEnumerable != null)
            {
                writer.WriteString(String.Join(",", nodeEnumerable.Select(n => n.Id)));
                return;
            }

            var nodes = data as IEnumerable;
            if (nodes != null)
            {
                var ids = new List<int>();
                foreach (Node n in nodes)
                    ids.Add(n.Id);
                writer.WriteString(String.Join(",", ids));
                return;
            }

            throw ExportNotSupportedException(GetData());
        }

        protected override void WriteXmlData(XmlWriter writer)
        {
            ExportData(writer, null);
        }

        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            var xmlNodeList = fieldNode.SelectNodes("*");

            if (!context.UpdateReferences)
            {
                if (xmlNodeList.Count > 0 || fieldNode.InnerText.Trim().Length > 0)
                    context.PostponedReferenceFields.Add(this.Name);
            }
            else
            {
                ImportData(fieldNode);
            }
        }

        protected override void ImportData(XmlNode fieldNode)
        {
            if (ImportDataId(fieldNode))
                return;
            ImportDataPath(fieldNode);
        }
        private void ImportDataPath(XmlNode fieldNode)
        {
            var list = new List<Node>();
            var xmlNodeList = fieldNode.SelectNodes("*");

            foreach (XmlNode refNode in xmlNodeList)
            {
                Node node = null;
                switch (refNode.LocalName)
                {
                    case "Path":
                        string path = refNode.InnerXml.Trim();
                        if (path.Length > 0)
                        {
                            if (!path.StartsWith(RepositoryPath.PathSeparator))
                                path = RepositoryPath.Combine(this.Content.ContentHandler.Path, path);
                            node = Node.LoadNode(path);
                            if (node == null)
                                throw new ReferenceNotFoundException("", String.Concat("Path: ", path));
                            list.Add(node);
                        }
                        break;
                    case "Id":
                        string idstring = refNode.InnerXml.Trim();
                        if (idstring.Length > 0)
                        {
                            int id;
                            if (!int.TryParse(refNode.InnerXml, out id))
                                throw base.InvalidImportDataException("Invalid Id");
                            node = Node.LoadNode(id);
                            if (node == null)
                                throw new ReferenceNotFoundException("", String.Concat("Id: ", id));
                            list.Add(node);
                        }
                        break;
                    default:
                        throw base.InvalidImportDataException("Unrecognized reference");
                }
            }
            if (fieldNode.InnerText.Trim().Length > 0 && list.Count == 0)
                throw this.InvalidImportDataException("ReferenceField.InnerText is not supported. Valid NodeContent is empty or <Path> or <Id> or <SearchExpression> element.");

            if (GetHandlerSlot(0) == typeof(IEnumerable))
                this.SetData(list);
            else if (GetHandlerSlot(0) == typeof(Node))
                this.SetData(list.Count == 0 ? null : list[0]);
            else
                throw new NotSupportedException(String.Concat("ReferenceField not supports this conversion: Node or IEnumerable to ", GetHandlerSlot(0).FullName));
        }
        private bool ImportDataId(XmlNode fieldNode)
        {
            if(fieldNode.SelectNodes("*").Count > 0)
                return false;

            var src = fieldNode.InnerText.Trim();
            if (src.Length == 0)
            {
                this.SetData(new Node[0]);
                return true;
            }

            var ids = src.Split(',').Select(s => int.Parse(s)).ToArray();
            var list = new NodeList<Node>(ids);
            if (GetHandlerSlot(0) == typeof(IEnumerable))
                this.SetData(list);
            else if (GetHandlerSlot(0) == typeof(Node))
                this.SetData(list.Count == 0 ? null : list.FirstOrDefault());
            else
                throw new NotSupportedException(String.Concat("ReferenceField not supports this conversion: Node or IEnumerable to ", GetHandlerSlot(0).FullName));

            return true;
        }

		protected override object ConvertTo(object[] handlerValues)
		{
			object handlerValue = handlerValues[0];
			if (handlerValue == null)
				return null;

			Node node = handlerValue as Node;
			if (node != null)
				return node;

			var enumerableHandlerValue = handlerValue as IEnumerable;
			if (enumerableHandlerValue != null)
			{
				var nodes = new List<Node>();
				foreach (var item in enumerableHandlerValue)
					nodes.Add((Node)item);
				return nodes;
			}

			throw new InvalidCastException(String.Format("Cannot cast {0} to {1} or {2}", handlerValue.GetType().FullName, typeof(IEnumerable), typeof(Node)));
		}
		protected override object[] ConvertFrom(object value)
		{
			if(value == null)
				return new object[1];
			
			var nodeValue = value as Node;
			var enumerableValue = value as IEnumerable;

            Type propertyType = this.GetHandlerSlot(0);
			if (propertyType == typeof(IEnumerable))
			{
				if (nodeValue != null)
					return new object[] { new Node[]{ nodeValue} };
				return new object[] { enumerableValue };
			}
			if (propertyType == typeof(Node))
			{
				if (nodeValue != null)
					return new object[] { nodeValue };
				Node node = null;
				// get first
				foreach (var n in enumerableValue)
				{
					node = n as Node;
					break;
				}
				return new object[] { node };
			}

			return new object[1];

        }

        protected override bool ParseValue(string value)
        {
            var refList = new List<Node>();
            foreach (var p in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var node = Node.LoadNodeByIdOrPath(p.Trim());
                if (node != null)
                    refList.Add(node);
            }

            var slotType = GetHandlerSlot(0);
            if (slotType == typeof(IEnumerable))
            {
                this.SetData(refList);
                return true;
            }
            else if (slotType == typeof(Node))
            {
                this.SetData(refList.Count == 0 ? null : refList[0]);
                return true;
            }

            throw new NotSupportedException(String.Concat("ReferenceField not supports this conversion: Node or IEnumerable to ", slotType.FullName));

        }

        public override bool HasValue()
        {
            var originalValue = OriginalValue;
            if (originalValue == null)
                return false;

            var node = originalValue as Node;
            if (node != null)
                return true;

            var nodes = originalValue as IEnumerable;
            return nodes != null && nodes.Cast<Node>().Any();
        }

        /*======================================================= IXmlChildList Members ====*/

        public string GetXmlChildName()
        {
            return "Path";
        }

        public IEnumerable<string> GetXmlChildValues()
        {
            if (this.Name == "Versions")
                if (!this.Content.Security.HasPermission(Storage.Security.PermissionType.RecallOldVersion))
                    return new string[0];

            var data = GetData();
            var node = data as Node;
            if (node != null)
                return new string[] { node.Path };

            var nodes = data as IEnumerable;
            if (nodes != null)
            {
                var paths = new List<string>();
                foreach (Node item in nodes)
                    paths.Add(item.Path);
                return paths;
            }
            return new string[0];
        }
    }
}

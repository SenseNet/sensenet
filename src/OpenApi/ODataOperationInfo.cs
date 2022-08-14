using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace SenseNet.OpenApi
{
    [DebuggerDisplay("{OperationName}")]
    public class ODataOperationInfo
    {
        private static string CR = Environment.NewLine;

        public bool IsValid { get; set; } = true;
        public bool IsDeprecated { get; set; }
        public bool IsAction { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string OperationName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Summary { get; set; }
        public string Documentation { get; set; }
        public List<string> ContentTypes { get; private set; } = new List<string>();
        public bool IsStatic { get; set; }
        public List<string> AllowedRoles { get; private set; } = new List<string>();
        public List<string> RequiredPermissions { get; private set; } = new List<string>();
        public List<string> RequiredPolicies { get; private set; } = new List<string>();
        public List<string> Scenarios { get; private set; } = new List<string>();
        public List<OperationParameterInfo> Parameters { get; } = new List<OperationParameterInfo>();
        public OperationParameterInfo ReturnValue { get; } = new OperationParameterInfo();

        public string Category { get; private set; }
        public string Url { get; set; }

        public ODataOperationInfo()
        {
        }

        public void Normalize()
        {
            OperationName = OperationName == null ? MethodName : OperationName.Trim('"');

            ContentTypes = NormalizeList(ContentTypes);
            IsStatic = ContentTypes.Count == 1 && ContentTypes[0] == "PortalRoot";
            AllowedRoles = NormalizeList(AllowedRoles);
            RequiredPermissions = NormalizeList(RequiredPermissions);
            RequiredPolicies = NormalizeList(RequiredPolicies);
            Scenarios = NormalizeList(Scenarios);

            Description = Description?.Trim('"').Trim();

            Url = IsStatic
                ? "/OData.svc/('Root')/" + OperationName
                : "/OData.svc/{_path}('{_name}')/" + OperationName;
        }

        public List<string> NormalizeList(List<string> list)
        {
            return list.SelectMany(x => x.Trim('"').Split(',').Select(y => y.Trim())).Distinct().ToList();
        }

        public void ParseDocumentation(XmlElement documentationElement)
        {
            if (documentationElement == null)
                return;

            ParseCategory(documentationElement);
            ParseLinks(documentationElement);
            ParseCode(documentationElement);
            ParseParameterDoc(documentationElement);
            ParseParagraphs(documentationElement);
            ParseExamples(documentationElement);
            ParseExceptions(documentationElement);

            var text = documentationElement.InnerXml;
            text = NormalizeWhitespaces(text);
            Documentation = text;
        }

        private void ParseCategory(XmlElement documentationElement)
        {
            var node = documentationElement.SelectSingleNode("snCategory");
            node?.ParentNode.RemoveChild(node);
            var category = node?.InnerText;
            if (string.IsNullOrEmpty(category))
                category = "Uncategorized";
            Category = category;
        }

        private void ParseParameterDoc(XmlElement documentationElement)
        {
            // <value>text</value> Replace with _text_
            foreach (var valueElement in documentationElement.SelectNodes("//value").OfType<XmlElement>().ToArray())
            {
                var innerXml = valueElement.InnerXml;
                if (string.IsNullOrEmpty(innerXml))
                    continue;

                var text = documentationElement.OwnerDocument.CreateTextNode($"_{innerXml}_");
                valueElement.ParentNode.ReplaceChild(text, valueElement);
            }

            // <paramref name=""> Replace with _name_
            foreach (var paramrefElement in documentationElement.SelectNodes("//paramref").OfType<XmlElement>().ToArray())
            {
                var name = paramrefElement.Attributes["name"]?.Value;
                if (name == null)
                    continue;

                var text = documentationElement.OwnerDocument.CreateTextNode($"_{name}_");
                paramrefElement.ParentNode.ReplaceChild(text, paramrefElement);
            }

            // <param name=""> Move to parameter's documentation
            foreach (var paramElement in documentationElement.SelectNodes("param").OfType<XmlElement>().ToArray())
            {
                var name = paramElement.Attributes["name"]?.Value;
                if (name == null)
                    continue;

                var parameter = Parameters.FirstOrDefault(x => x.Name == name);
                if (parameter == null)
                    continue;

                var example = paramElement.Attributes["example"]?.Value;
                if (example != null)
                    parameter.Example = example;

                parameter.Documentation = paramElement.InnerXml;
                documentationElement.RemoveChild(paramElement);
            }

            // <returns> Move to ReturnValue's documentation
            var returnElement = documentationElement.SelectSingleNode("returns");
            if (returnElement == null)
                return;
            ReturnValue.Documentation = returnElement.InnerXml;
            documentationElement.RemoveChild(returnElement);
        }

        private void ParseCode(XmlElement documentationElement)
        {
            foreach (var element in documentationElement.SelectNodes("//c").OfType<XmlElement>().ToArray())
            {
                var text = documentationElement.OwnerDocument.CreateTextNode($"`{element.InnerXml}`");
                element.ParentNode.ReplaceChild(text, element);
            }
            foreach (var element in documentationElement.SelectNodes("//code").OfType<XmlElement>().ToArray())
            {
                var src = element.InnerXml.TrimEnd(' ', '\t');

                var cr1 = src.StartsWith("\r") || src.StartsWith("\n") ? "" : CR;
                var cr2 = src.EndsWith("\r") || src.EndsWith("\n") ? "" : CR;

                var lang = element.Attributes["lang"]?.Value ?? string.Empty;

                var text = documentationElement.OwnerDocument.CreateTextNode($"``` {lang}{cr1}{src}{cr2}```{CR}");

                element.ParentNode.ReplaceChild(text, element);
            }
        }

        private void ParseLinks(XmlElement documentationElement)
        {
            // <seealso cref=""> Replace with _cref_
            // <see cref=""> Replace with _cref_
            var nodes = documentationElement.SelectNodes("//seealso").OfType<XmlElement>()
                .Union(documentationElement.SelectNodes("//see").OfType<XmlElement>())
                .ToArray();
            foreach (var element in nodes)
            {
                var cref = element.Attributes["cref"]?.Value;
                if (cref == null)
                    continue;

                var text = documentationElement.OwnerDocument.CreateTextNode($"_{cref}_");
                element.ParentNode.ReplaceChild(text, element);
            }
        }

        private void ParseParagraphs(XmlElement documentationElement)
        {
            // <nodoc>... Remove these nodes
            foreach (var element in documentationElement.SelectNodes("//nodoc").OfType<XmlElement>().ToArray())
            {
                element.ParentNode.RemoveChild(element);
            }
            // <para>... Replace with a newline + inner text.
            foreach (var element in documentationElement.SelectNodes("//para").OfType<XmlElement>().ToArray())
            {
                var text = documentationElement.OwnerDocument.CreateTextNode(CR + CR + element.InnerText + CR + CR);
                element.ParentNode.ReplaceChild(text, element);
            }
            // <summary>... Replace with a newline + inner text.
            foreach (var element in documentationElement.SelectNodes("summary").OfType<XmlElement>().ToArray())
            {
                this.Summary = element.InnerText;
                var text = documentationElement.OwnerDocument.CreateTextNode(CR + CR + element.InnerText + CR + CR);
                element.ParentNode.ReplaceChild(text, element);
            }
            // <remarks>... Replace with a newline + inner text.
            foreach (var element in documentationElement.SelectNodes("remarks").OfType<XmlElement>().ToArray())
            {
                var text = documentationElement.OwnerDocument.CreateTextNode(CR + CR + element.InnerText + CR + CR);
                element.ParentNode.ReplaceChild(text, element);
            }
        }

        private void ParseExamples(XmlElement documentationElement)
        {
            var sb = new StringBuilder();
            // <example>... Move to end
            var elements = documentationElement.SelectNodes("example").OfType<XmlElement>().ToArray();
            foreach (var element in elements)
            {
                if (sb.Length == 0)
                    sb.AppendLine().Append("### Example").AppendLine(elements.Length > 1 ? "s" : "");
                sb.AppendLine();
                sb.AppendLine(element.InnerText);
                element.ParentNode.RemoveChild(element);
            }
            var text = documentationElement.OwnerDocument.CreateTextNode(sb.ToString());
            documentationElement.AppendChild(text);
        }

        private void ParseExceptions(XmlElement documentationElement)
        {
            var sb = new StringBuilder();
            // <exception>... Move to end
            var elements = documentationElement.SelectNodes("exception").OfType<XmlElement>().ToArray();
            foreach (var element in elements)
            {
                var cref = element.Attributes["cref"]?.Value;
                if (cref == null)
                    continue;

                if (sb.Length == 0)
                    sb.AppendLine().Append("### Exception").AppendLine(elements.Length > 1 ? "s" : "");

                sb.AppendLine($"- {cref}: {element.InnerText}");
                element.ParentNode.RemoveChild(element);
            }
            var text = documentationElement.OwnerDocument.CreateTextNode(sb.ToString());
            documentationElement.AppendChild(text);
        }

        private string NormalizeWhitespaces(string text)
        {
            var lines = text
                .Trim()
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n')
                //.Select(x => x.Trim())
                ;

            var result = new List<string>();
            var emptyLines = 0;
            foreach (var line in lines)
            {
                if (line.Length == 0)
                {
                    if (++emptyLines > 1)
                        continue;
                }
                else
                {
                    emptyLines = 0;
                }
                result.Add(line);
            }

            return string.Join(CR, result);
        }
    }
}

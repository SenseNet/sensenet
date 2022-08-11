using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace SenseNet.OpenApi
{
    [DebuggerDisplay("{OperationName}")]
    internal class ODataOperationInfo
    {
        private static string CR = Environment.NewLine;
        private readonly XmlElement _documentationElement;

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

        public ODataOperationInfo(XmlElement documentationElement)
        {
            _documentationElement = documentationElement;
        }

        public void Normalize()
        {
            OperationName = OperationName == null ? MethodName : OperationName.Trim('"');

            ContentTypes = NormalizeList(ContentTypes);
            IsStatic = ContentTypes.Count == 1 && ContentTypes[0] == "N.CT.PortalRoot";
            AllowedRoles = NormalizeList(AllowedRoles);
            RequiredPermissions = NormalizeList(RequiredPermissions);
            RequiredPolicies = NormalizeList(RequiredPolicies);
            Scenarios = NormalizeList(Scenarios);

            Description = Description?.Trim('"').Trim();
        }

        public List<string> NormalizeList(List<string> list)
        {
            return list.SelectMany(x => x.Trim('"').Split(',').Select(y => y.Trim())).Distinct().ToList();
        }

        public void ParseDocumentation()
        {
            if (_documentationElement == null)
                return;

            ParseCategory();
            ParseLinks();
            ParseCode();
            ParseParameterDoc();
            ParseParagraphs();
            ParseExamples();
            ParseExceptions();

            var text = _documentationElement.InnerXml;
            text = NormalizeWhitespaces(text);
            Documentation = text;
        }

        private void ParseCategory()
        {
            var node = _documentationElement.SelectSingleNode("snCategory");
            node?.ParentNode.RemoveChild(node);
            var category = node?.InnerText;
            if (string.IsNullOrEmpty(category))
                category = "Uncategorized";
            Category = category;
        }

        private void ParseParameterDoc()
        {
            // <value>text</value> Replace with _text_
            foreach (var valueElement in _documentationElement.SelectNodes("//value").OfType<XmlElement>().ToArray())
            {
                var innerXml = valueElement.InnerXml;
                if (string.IsNullOrEmpty(innerXml))
                    continue;

                var text = _documentationElement.OwnerDocument.CreateTextNode($"_{innerXml}_");
                valueElement.ParentNode.ReplaceChild(text, valueElement);
            }

            // <paramref name=""> Replace with _name_
            foreach (var paramrefElement in _documentationElement.SelectNodes("//paramref").OfType<XmlElement>().ToArray())
            {
                var name = paramrefElement.Attributes["name"]?.Value;
                if (name == null)
                    continue;

                var text = _documentationElement.OwnerDocument.CreateTextNode($"_{name}_");
                paramrefElement.ParentNode.ReplaceChild(text, paramrefElement);
            }

            // <param name=""> Move to parameter's documentation
            foreach (var paramElement in _documentationElement.SelectNodes("param").OfType<XmlElement>().ToArray())
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
                _documentationElement.RemoveChild(paramElement);
            }

            // <returns> Move to ReturnValue's documentation
            var returnElement = _documentationElement.SelectSingleNode("returns");
            if (returnElement == null)
                return;
            ReturnValue.Documentation = returnElement.InnerXml;
            _documentationElement.RemoveChild(returnElement);
        }

        private void ParseCode()
        {
            foreach (var element in _documentationElement.SelectNodes("//c").OfType<XmlElement>().ToArray())
            {
                var text = _documentationElement.OwnerDocument.CreateTextNode($"`{element.InnerXml}`");
                element.ParentNode.ReplaceChild(text, element);
            }
            foreach (var element in _documentationElement.SelectNodes("//code").OfType<XmlElement>().ToArray())
            {
                var src = element.InnerXml.TrimEnd(' ', '\t');

                var cr1 = src.StartsWith("\r") || src.StartsWith("\n") ? "" : CR;
                var cr2 = src.EndsWith("\r") || src.EndsWith("\n") ? "" : CR;

                var lang = element.Attributes["lang"]?.Value ?? string.Empty;

                var text = _documentationElement.OwnerDocument.CreateTextNode($"``` {lang}{cr1}{src}{cr2}```{CR}");

                element.ParentNode.ReplaceChild(text, element);
            }
        }

        private void ParseLinks()
        {
            // <seealso cref=""> Replace with _cref_
            // <see cref=""> Replace with _cref_
            var nodes = _documentationElement.SelectNodes("//seealso").OfType<XmlElement>()
                .Union(_documentationElement.SelectNodes("//see").OfType<XmlElement>())
                .ToArray();
            foreach (var element in nodes)
            {
                var cref = element.Attributes["cref"]?.Value;
                if (cref == null)
                    continue;

                var text = _documentationElement.OwnerDocument.CreateTextNode($"_{cref}_");
                element.ParentNode.ReplaceChild(text, element);
            }
        }

        private void ParseParagraphs()
        {
            // <nodoc>... Remove these nodes
            foreach (var element in _documentationElement.SelectNodes("//nodoc").OfType<XmlElement>().ToArray())
            {
                element.ParentNode.RemoveChild(element);
            }
            // <para>... Replace with a newline + inner text.
            foreach (var element in _documentationElement.SelectNodes("//para").OfType<XmlElement>().ToArray())
            {
                var text = _documentationElement.OwnerDocument.CreateTextNode(CR + CR + element.InnerText + CR + CR);
                element.ParentNode.ReplaceChild(text, element);
            }
            // <summary>... Replace with a newline + inner text.
            foreach (var element in _documentationElement.SelectNodes("summary").OfType<XmlElement>().ToArray())
            {
                this.Summary = element.InnerText;
                var text = _documentationElement.OwnerDocument.CreateTextNode(CR + CR + element.InnerText + CR + CR);
                element.ParentNode.ReplaceChild(text, element);
            }
            // <remarks>... Replace with a newline + inner text.
            foreach (var element in _documentationElement.SelectNodes("remarks").OfType<XmlElement>().ToArray())
            {
                var text = _documentationElement.OwnerDocument.CreateTextNode(CR + CR + element.InnerText + CR + CR);
                element.ParentNode.ReplaceChild(text, element);
            }
        }

        private void ParseExamples()
        {
            var sb = new StringBuilder();
            // <example>... Move to end
            var elements = _documentationElement.SelectNodes("example").OfType<XmlElement>().ToArray();
            foreach (var element in elements)
            {
                if (sb.Length == 0)
                    sb.AppendLine().Append("### Example").AppendLine(elements.Length > 1 ? "s" : "");
                sb.AppendLine();
                sb.AppendLine(element.InnerText);
                element.ParentNode.RemoveChild(element);
            }
            var text = _documentationElement.OwnerDocument.CreateTextNode(sb.ToString());
            _documentationElement.AppendChild(text);
        }

        private void ParseExceptions()
        {
            var sb = new StringBuilder();
            // <exception>... Move to end
            var elements = _documentationElement.SelectNodes("exception").OfType<XmlElement>().ToArray();
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
            var text = _documentationElement.OwnerDocument.CreateTextNode(sb.ToString());
            _documentationElement.AppendChild(text);
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

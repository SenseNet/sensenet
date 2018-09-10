using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml.XPath;
using System.Xml;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository.Xpath
{
    [DebuggerDisplay("<{Name} : {NodeType}")]
    internal abstract class ElementBase
    {
        public NavigatorContext Context { get; private set; }
        public Content Content { get; private set; }

        public string Name { get; private set; }
        public ElementBase Parent { get; private set; }
        public ElementBase FirstChild { get; private set; }
        public ElementBase FollowingSibling { get; private set; }
        public ElementBase PrecedingSibling { get; private set; }

        public abstract bool IsEmpty { get; }
        public virtual XPathNodeType NodeType { get { return XPathNodeType.Element; } }

        public ElementBase(NavigatorContext context, Content content, string name, ElementBase parent)
        {
            Name = name;
            Parent = parent;
            Context = context;
            Content = content;
        }

        private bool _firstChildCompleted;
        private bool _siblingsCompleted;
        public ElementBase GetFirstChild()
        {
            if (!_firstChildCompleted)
            {
                FirstChild = CreateFirstChild();
                _firstChildCompleted = true;
            }
            else
            {
            }
            return FirstChild;
        }
        public abstract ElementBase CreateFirstChild();

        public ElementBase GetNextElement()
        {
            if (FollowingSibling != null)
                return FollowingSibling;
            if(_siblingsCompleted)
                return FollowingSibling;

            var element = CreateNextElement();
            if (element != null)
            {
                element.PrecedingSibling = this;
                this.FollowingSibling = element;
            }
            else
            {
                _siblingsCompleted = true;
            }
            return element;
        }
        public abstract ElementBase CreateNextElement();
        public ElementBase GetPreviousElement()
        {
            return PrecedingSibling;
        }

        public virtual string[] GetAttributeNames()
        {
            return null;
        }
        public virtual string GetAttributeValue(string name)
        {
            return string.Empty;
        }

        public string CollectTextValue()
        {
            var sb = new StringBuilder();
            CollectTextValue(sb);
            return sb.ToString();
        }
        protected virtual void CollectTextValue(StringBuilder sb)
        {
            var child = GetFirstChild();
            if (child != null)
            {
                child.CollectTextValue(sb);
                while ((child = child.FollowingSibling) != null)
                    child.CollectTextValue(sb);
            }
        }

        internal void RemoveHashSignFromName()
        {
            Name = Name.Substring(1);
        }
    }
    internal abstract class ContainerElement : ElementBase
    {
        public ContainerElement(NavigatorContext context, Content content, string name, ElementBase parent) : base(context, content, name, parent) { }
    }

    internal class RootElement : ContainerElement
    {
        public override bool IsEmpty { get { return false; } }
        public override XPathNodeType NodeType { get { return XPathNodeType.Root; } }

        public RootElement(NavigatorContext context, Content content) : base(context, content, "#document", null)
        {
            context.SetRoot(this);
        }

        public override ElementBase CreateFirstChild()
        {
            return new ContentElement(this.Context, Context.MainContent, this, false, 0);
        }
        public override ElementBase CreateNextElement()
        {
            return null;
        }
    }

    internal class ChildrenElement : ContainerElement
    {
        public override bool IsEmpty { get { return Context.Children.Length == 0; } }

        public ChildrenElement(NavigatorContext context, Content content, ElementBase parent) : base(context, content, "Children", parent) { }

        public override ElementBase CreateFirstChild()
        {
            if (IsEmpty)
                return null;
            var content = Context.Children[0];
            return new ContentElement(this.Context, content, this, true, 0);
        }
        public override ElementBase CreateNextElement() { return null; }
    }
    internal class ContentElement : ContainerElement
    {
        public override bool IsEmpty { get { return false; } }
        public bool IsChildContent { get; private set; }
        public int ContentIndex { get; private set; }

        public ContentElement(NavigatorContext context, Content content, ElementBase parent, bool isChildContent, int contentIndex)
            : base(context, content, "Content", parent)
        {
            IsChildContent = isChildContent;
            ContentIndex = contentIndex;
        }

        public override ElementBase CreateFirstChild()
        {
            return new ContentHeadElement(this.Context, this.Content, "ContentType", this);
        }
        public override ElementBase CreateNextElement()
        {
            if (!IsChildContent)
                return null;
            var index = ContentIndex + 1;
            if (index == Context.Children.Length)
                return null;
            var content = Context.Children[index];
            return new ContentElement(Context, content, Parent, true, index);
        }
    }
    internal class TextElement : ElementBase
    {
        public override XPathNodeType NodeType { get { return XPathNodeType.Text; } }
        public override bool IsEmpty { get { return true; } }
        private string _textValue;

        public TextElement(NavigatorContext context, Content content, ElementBase parent, string textValue)
            : base(context, content, "#text", parent)
        {
            _textValue = textValue;
        }

        public override ElementBase CreateFirstChild() { return null; }
        public override ElementBase CreateNextElement() { return null; }

        protected override void CollectTextValue(StringBuilder sb)
        {
            sb.Append(_textValue);
        }
    }
    internal class ContentHeadElement : ElementBase
    {
        public override bool IsEmpty { get { return String.IsNullOrEmpty(GetValue()); } }

        public ContentHeadElement(NavigatorContext context, Content content, string name, ElementBase parent) : base(context, content, name, parent) { }

        private string _value;
        private bool? _hasValue;
        private string GetValue()
        {
            if (_hasValue != null)
                return _value;
            _value = GetFieldValue();
            _hasValue = _value != null;
            return _value;
        }
        private string GetFieldValue()
        {
            var contentType = Content.ContentType;
            switch (Name)
            {
                case "ContentType": return contentType == null ? String.Empty : contentType.Name;
                case "ContentTypePath": return contentType == null ? String.Empty : contentType.Path;
                case "ContentTypeTitle": return contentType == null ? String.Empty : contentType.DisplayName;
                case "ContentName": return Content.Name;
                case "Icon": return contentType.Icon;
                case "SelfLink": return this.Content.Path;
                case "IsFolder": return (this.Content.ContentHandler is IFolder).ToString().ToLowerInvariant();
                default:
                    throw new SnNotSupportedException();
            }
        }

        public override ElementBase CreateFirstChild()
        {
            if (GetValue() == null)
                return null;
            return new TextElement(Context, Content, this, GetValue());
        }
        public override ElementBase CreateNextElement()
        {
            switch (Name)
            {
                case "ContentType": return new ContentHeadElement(this.Context, this.Content, "ContentTypePath", this.Parent);
                case "ContentTypePath": return new ContentHeadElement(this.Context, this.Content, "ContentTypeTitle", this.Parent);
                case "ContentTypeTitle": return new ContentHeadElement(this.Context, this.Content, "ContentName", this.Parent);
                case "ContentName": return new ContentHeadElement(this.Context, this.Content, "Icon", this.Parent);
                case "Icon": return new ContentHeadElement(this.Context, this.Content, "SelfLink", this.Parent);
                case "SelfLink": return new ContentHeadElement(this.Context, this.Content, "IsFolder", this.Parent);
                case "IsFolder": return new FieldsElement(this.Context, this.Content, this.Parent);
                default:
                    throw new SnNotSupportedException("##");
            }
        }
    }
    internal class FieldsElement : ContainerElement
    {
        internal IEnumerator<Field> FieldEnumerator { get; private set; }

        private bool _isEmpty;
        public override bool IsEmpty { get { return _isEmpty; } }

        public FieldsElement(NavigatorContext context, Content content, ElementBase parent)
            : base(context, content, "Fields", parent)
        {
            var enumerator = content.Fields.Values.AsEnumerable<Field>().GetEnumerator();
            FieldEnumerator = new FieldEnumerator(enumerator);
            _isEmpty = !FieldEnumerator.MoveNext();

        }

        public override ElementBase CreateFirstChild()
        {
            if (this.IsEmpty)
                return null;
            return FieldElement.Create(Context, Content, this, FieldEnumerator.Current);
        }
        public override ElementBase CreateNextElement()
        {
            return new ActionsElement(this.Context, this.Content, this.Parent);
        }
    }
    internal abstract class FieldElement : ElementBase
    {
        private IXmlAttributeOwner _attributeContainer;
        private bool _isListField;

        public Field Field { get; private set; }

        protected FieldElement(NavigatorContext context, Content content, string name, ElementBase parent, Field field)
            : base(context, content, name, parent)
        {
            this.Field = field;
            _attributeContainer = field as IXmlAttributeOwner;
            _isListField = field.Name[0] == '#';
            if (_isListField)
                base.RemoveHashSignFromName();
        }

        public override string[] GetAttributeNames()
        {
            if (_isListField)
            {
                if (_attributeContainer == null)
                    return new[] { Field.FIELDSUBTYPEATTRIBUTENAME };
                var names = _attributeContainer.GetXmlAttributeNames().ToList();
                names.Add(Field.FIELDSUBTYPEATTRIBUTENAME);
                return names.ToArray();
            }
            if (_attributeContainer != null)
                return _attributeContainer.GetXmlAttributeNames().ToArray();
            return null;
        }
        public override string GetAttributeValue(string name)
        {
            if (name == Field.FIELDSUBTYPEATTRIBUTENAME)
                return "ContentList";
            return _attributeContainer.GetXmlAttribute(name);
        }

        public override ElementBase CreateNextElement()
        {
            var enumerator = ((FieldsElement)Parent).FieldEnumerator;
            var hasNextField = enumerator.MoveNext();
            if (!hasNextField)
                return null;
            return FieldElement.Create(Context, Content, Parent, enumerator.Current);
        }
        internal static FieldElement Create(NavigatorContext context, Content content, ElementBase parent, Field field)
        {
            if (field is IXmlChildList)
                return new ItemContainerElement(context, content, field.Name, parent, field);
            if (field is IRawXmlContainer)
                return new XmlFieldElement(context, content, field.Name, parent, field);
            return new SimpleFieldElement(context, content, field.Name, parent, field);
        }
    }
    internal class SimpleFieldElement : FieldElement
    {
        public override bool IsEmpty { get { return String.IsNullOrEmpty(GetValue()); } }

        public SimpleFieldElement(NavigatorContext context, Content content, string name, ElementBase parent, Field field) : base(context, content, name, parent, field) { }

        private string _value;
        private bool? _hasValue;
        public string GetValue()
        {
            if (_hasValue != null)
                return _value;
            _value = GetFieldValue();
            _hasValue = _value != null;
            return _value;
        }
        private string GetFieldValue()
        {
            var value = Field.GetInnerXml();
            if (String.IsNullOrEmpty(value))
                return null;
            return value;
        }

        public override ElementBase CreateFirstChild()
        {
            if (GetValue() == null)
                return null;
            return new TextElement(Context, Content, this, GetValue());
        }
    }
    internal class ItemContainerElement : FieldElement
    {
        internal IEnumerator<string> ChildValueEnumerator { get; private set; }
        internal string ChildItemName { get; private set; }

        private bool _isEmpty;
        public override bool IsEmpty { get { return _isEmpty; } }

        public ItemContainerElement(NavigatorContext context, Content content, string name, ElementBase parent, Field field)
            : base(context, content, name, parent, field)
        {
            var listField = (IXmlChildList)field;
            ChildItemName = listField.GetXmlChildName();
            ChildValueEnumerator = listField.GetXmlChildValues().GetEnumerator();
            _isEmpty = !ChildValueEnumerator.MoveNext();
        }

        public override ElementBase CreateFirstChild()
        {
            if (this.IsEmpty)
                return null;
            return new ChildItemElement(Context, Content, this, ChildItemName, ChildValueEnumerator.Current);

        }
    }
    internal class ChildItemElement : ElementBase
    {
        private string _value;

        public override bool IsEmpty { get { return false; } }

        public ChildItemElement(NavigatorContext context, Content content, ElementBase parent, string name, string value)
            : base(context, content, name, parent)
        {
            _value = value;
        }

        public override ElementBase CreateFirstChild()
        {
            if (_value == null)
                return null;
            return new TextElement(Context, Content, this, _value);
        }
        public override ElementBase CreateNextElement()
        {
            var enumerator = ((ItemContainerElement)Parent).ChildValueEnumerator;
            var hasNextItem = enumerator.MoveNext();
            if (!hasNextItem)
                return null;
            return new ChildItemElement(Context, Content, Parent, this.Name, enumerator.Current);
        }
    }
    internal class ActionsElement : ContainerElement
    {
        internal IEnumerator<ActionBase> ActionEnumerator { get; private set; }
        private bool _isEmpty;
        public override bool IsEmpty { get { return _isEmpty; } }

        public ActionsElement(NavigatorContext context, Content content, ElementBase parent)
            : base(context, content, "Actions", parent)
        {
            ActionEnumerator = ActionFramework.GetActionsForContentNavigator(content).GetEnumerator();
            _isEmpty = !ActionEnumerator.MoveNext();
        }

        public override ElementBase CreateFirstChild()
        {
            if (this.IsEmpty)
                return null;
            return ActionElement.Create(Context, Content, this, ActionEnumerator.Current);
        }
        public override ElementBase CreateNextElement()
        {
            if (((ContentElement)Parent).IsChildContent)
                return null;
            if (!Context.WithChildren)
                return null;
            var mainContent = Context.MainContent;
            if (mainContent.Children == null)
                return null;
            if (mainContent.Children.Count() == 0)
                return null;
            return new ChildrenElement(this.Context, this.Content, this.Parent);
        }
    }
    internal class ActionElement : ElementBase
    {
        private ActionBase _action;

        public override bool IsEmpty { get { return false; } }

        private ActionElement(NavigatorContext context, Content content, string name, ElementBase parent, ActionBase action)
            : base(context, content, name, parent)
        {
            _action = action;
        }

        public override string[] GetAttributeNames()
        {
            if (!_action.IncludeBackUrl)
                return new[] { ActionBase.BackUrlParameterName };

            return null;
        }
        public override string GetAttributeValue(string name)
        {
            if (name == ActionBase.BackUrlParameterName)
                return _action.BackUrlWithParameter;

            return string.Empty;
        }

        public override ElementBase CreateFirstChild()
        {
            return new TextElement(Context, Content, this, _action.Uri);
        }
        public override ElementBase CreateNextElement()
        {
            var enumerator = ((ActionsElement)Parent).ActionEnumerator;
            var hasNextAction = enumerator.MoveNext();
            if (!hasNextAction)
                return null;
            return ActionElement.Create(Context, Content, Parent, enumerator.Current);
        }

        internal static ActionElement Create(NavigatorContext context, Content content, ElementBase parent, ActionBase action)
        {
            return new ActionElement(context, content, action.Name, parent, action);
        }
    }

    /*-----------------------------------------------------------------*/

    internal class XmlNodeWrapper : ElementBase
    {
        private XmlNode _wrappedNode;

        public XmlNodeWrapper(NavigatorContext context, Content content, string name, ElementBase parent, XmlNode wrappedNode)
            : base(context, content, name, parent)
        {
            _wrappedNode = wrappedNode;
        }

        public override bool IsEmpty
        {
            get { throw new SnNotSupportedException(); }
        }
        public override XPathNodeType NodeType
        {
            get
            {
                switch (_wrappedNode.NodeType)
                {
                    case XmlNodeType.Attribute: return XPathNodeType.Attribute;
                    case XmlNodeType.CDATA: return XPathNodeType.Text;
                    case XmlNodeType.Comment: return XPathNodeType.Comment;
                    case XmlNodeType.Element: return XPathNodeType.Element;
                    case XmlNodeType.ProcessingInstruction: return XPathNodeType.ProcessingInstruction;
                    case XmlNodeType.SignificantWhitespace: return XPathNodeType.SignificantWhitespace;
                    case XmlNodeType.Text: return XPathNodeType.Text;
                    case XmlNodeType.Whitespace: return XPathNodeType.Whitespace;
                    default:
                        throw new NotSupportedException("Not supported NodeType: " + _wrappedNode.NodeType);
                }
            }
        }
        public override string[] GetAttributeNames()
        {
            var attrs = _wrappedNode.Attributes;
            if (attrs.Count == 0)
                return null;
            var names = new string[attrs.Count];
            for (int i = 0; i < attrs.Count; i++)
                names[i] = attrs[i].Name;
            return names;
        }
        public override string GetAttributeValue(string name)
        {
            return _wrappedNode.Attributes[name].Value;
        }
        public string Value
        {
            get { return _wrappedNode.Value; }
        }

        protected override void CollectTextValue(StringBuilder sb)
        {
            sb.Append(_wrappedNode.InnerText);
        }

        public override ElementBase CreateFirstChild()
        {
            var firstChild = _wrappedNode.FirstChild;
            if (firstChild == null)
                return null;
            return XmlNodeWrapper.Create(Context, Content, firstChild.LocalName, this, firstChild);
        }
        public override ElementBase CreateNextElement()
        {
            var node = _wrappedNode.NextSibling;
            if (node == null)
                return null;

            return Create(Context, Content, node.Name, Parent, node);
        }

        internal static XmlNodeWrapper Create(NavigatorContext context, Content content, string name, ElementBase parent, XmlNode wrappedNode)
        {
            return new XmlNodeWrapper(context, content, name, parent, wrappedNode);
        }
    }
    internal class XmlFieldElement : FieldElement
    {
        private const string INNERNAVIGATORROOTELEMENTNAME = "innerdocumentroot";
        private XmlDocument __innerDocument;
        private XmlDocument InnerDocument
        {
            get
            {
                if (__innerDocument == null)
                {
                    __innerDocument = new XmlDocument();
                    __innerDocument.LoadXml(String.Concat("<", INNERNAVIGATORROOTELEMENTNAME, ">",
                        ((IRawXmlContainer)this.Field).GetRawXml(), "</", INNERNAVIGATORROOTELEMENTNAME, ">"));
                }
                return __innerDocument;
            }
        }

        public override bool IsEmpty
        {
            get { return String.IsNullOrEmpty(FieldValue); }
        }

        private string _fieldValue;
        private bool? _hasFieldValue;
        public string FieldValue
        {
            get
            {
                if (_hasFieldValue != null)
                    return _fieldValue;
                _fieldValue = this.Field.GetData().ToString();
                _hasFieldValue = _fieldValue != null;
                return _fieldValue;
            }
        }

        private string _value;
        private bool? _hasValue;
        public string GetValue()
        {
            if (_hasValue != null)
                return _value;
            _value = GetValue1();
            _hasValue = _value != null;
            return _value;
        }
        private string GetValue1()
        {
            return InnerDocument.DocumentElement.InnerText;
        }

        protected override void CollectTextValue(StringBuilder sb)
        {
            sb.Append(InnerDocument.DocumentElement.InnerText);
        }

        public XmlFieldElement(NavigatorContext context, Content content, string name, ElementBase parent, Field field) : base(context, content, name, parent, field) { }

        public override ElementBase CreateFirstChild()
        {
            if (GetValue() == null)
                return null;
            var firstChild = InnerDocument.DocumentElement.FirstChild;
            return XmlNodeWrapper.Create(Context, Content, firstChild.LocalName, this, firstChild);
        }
    }

    /*=================================================================*/

    internal class FieldEnumerator : IEnumerator<Field>
    {
        private IEnumerator<Field> _wrappedEnumerator;

        public FieldEnumerator(IEnumerator<Field> enumerator)
        {
            _wrappedEnumerator = enumerator;
        }

        public Field Current
        {
            get { return _wrappedEnumerator.Current; }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }
        public bool MoveNext()
        {
            while (true)
            {
                if (!_wrappedEnumerator.MoveNext())
                    return false;
                if (CurrentIsAllowed())
                    break;
            }
            return true;
        }
        private bool CurrentIsAllowed()
        {
            var field = Current;
            if (field.Name == "Name")
                return false;
            return true;
        }

        public void Reset()
        {
            _wrappedEnumerator.Reset();
        }

        public void Dispose()
        {
        }
    }
}

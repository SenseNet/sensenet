using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.Xml;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Xpath
{
    /// <summary>
    /// Provides an XmlAttribute accessing mechanism for the inner XPathNavigator.
    /// Implement this interface on a <see cref="SenseNet.ContentRepository.Field"/>
    /// that provides any XmlAttribute.
    /// </summary>
    public interface IXmlAttributeOwner
    {
        /// <summary>
        /// Returns all attribute names
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetXmlAttributeNames();
        /// <summary>
        /// Returns the named attribute value.
        /// </summary>
        /// <param name="name">Name of the desired attribute</param>
        /// <returns>Value of the attribute. Can be null or empty.</returns>
        string GetXmlAttribute(string name);
    }
    /// <summary>
    /// Provides a data source of a simple list of items for the inner XPathNavigator.
    /// Implement this interface on a <see cref="SenseNet.ContentRepository.Field"/> that contains
    /// one or more simple values. Every value will be enveloped by an XmlElement.
    /// </summary>
    public interface IXmlChildList
    {
        /// <summary>
        /// Name of the XmlElement that will envelope an item.
        /// </summary>
        /// <returns></returns>
        string GetXmlChildName();
        /// <summary>
        /// Enumerable set of item values.
        /// </summary>
        /// <returns>String</returns>
        IEnumerable<string> GetXmlChildValues();
    }
    /// <summary>
    /// Provides a method for accessing a raw xml fragment for the inner XPathNavigator.
    /// Implement this interface on a <see cref="SenseNet.ContentRepository.Field"/> that has
    /// a complex XML structure.
    /// </summary>
    public interface IRawXmlContainer
    {
        /// <summary>
        /// Returns the <see cref="SenseNet.ContentRepository.Field"/>'s XML structure.
        /// </summary>
        /// <returns>String</returns>
        string GetRawXml();
    }

    internal class NavigatorContext
    {
        public Content MainContent { get; set; }
        public bool WithChildren { get; set; }

        private Content[] _children;
        public Content[] Children
        {
            get
            {
                if (_children != null)
                    return _children;
                _children = WithChildren ? MainContent.Children.ToArray() : new Content[0];
                return _children;
            }
        }

        internal RootElement Root { get; private set; }

        internal void SetRoot(RootElement rootElement)
        {
            Root = rootElement;
        }
    }

    /// <summary>
    /// Wrapper class of the <see cref="SenseNet.ContentRepository.Content"/>.
    /// Implements the <see cref="System.Xml.XPath.IXPathNavigable"/>
    /// for accessing an <see cref="System.Xml.XPath.XPathNavigator"/>
    /// </summary>
    public class NavigableContent : IXPathNavigable
    {
        private Content _content;
        /// <summary>
        /// Creates an instance of the NavigableContent.
        /// </summary>
        /// <param name="content">Wrapped <see cref="SenseNet.ContentRepository.Content"/> instance.</param>
        public NavigableContent(Content content)
        {
            _content = content;
        }
        /// <summary>
        /// Creates a <see cref="System.Xml.XPath.XPathNavigator"/> instance.
        /// </summary>
        /// <returns>The navigator instance.</returns>
        public XPathNavigator CreateNavigator()
        {
            return new ContentNavigator(_content, true);
        }
    }

    internal class ContentNavigator : XPathNavigator
    {
        public ContentNavigator(Content content, bool withChildren)
        {
            var context = new NavigatorContext();
            context.MainContent = content;
            context.WithChildren = withChildren;
            _currentElement = new RootElement(context, content);
        }

        private ContentNavigator() { }

        private string[] _attrNames;
        public int _attrIndex = -1;
        private ElementBase _currentElement;

        public override XPathNavigator Clone()
        {
            var clone = new ContentNavigator(/*Context*/);
            clone._currentElement = this._currentElement;
            clone._attrNames = this._attrNames;
            clone._attrIndex = this._attrIndex;
            return clone;
        }

        public override bool IsSamePosition(XPathNavigator other)
        {
            var cnav = other as ContentNavigator;
            if (cnav == null)
                return false;

            if (this._currentElement.Context != cnav._currentElement.Context)
                return false;

            if (_currentElement != cnav._currentElement)
                return false;
            if (_attrIndex != cnav._attrIndex)
                return false;
            return true;
        }
        public override bool MoveTo(XPathNavigator other)
        {
            var cnav = other as ContentNavigator;
            if (cnav == null)
                return false;

            if (this._currentElement.Context != cnav._currentElement.Context)
                return false;

            if (_attrIndex != cnav._attrIndex)
                _attrIndex = cnav._attrIndex;
            if (_attrNames != cnav._attrNames)
                _attrNames = cnav._attrNames;
            if (_currentElement != cnav._currentElement)
                _currentElement = cnav._currentElement;
            return true;
        }

        public override bool MoveToFirstChild()
        {
            if (_attrIndex > -1)
                return false;

            var child = _currentElement.GetFirstChild();
            if (child == null)
                return false;

            _currentElement = child;
            return true;
        }
        public override bool MoveToNext()
        {
            if (_attrIndex > -1)
                return false;

            var next = _currentElement.GetNextElement();
            if (next == null)
                return false;
            _currentElement = next;
            return true;
        }
        public override bool MoveToParent()
        {
            if (_attrIndex > -1)
            {
                _attrIndex = -1;
                return true;
            }
            var parent = _currentElement.Parent;
            if (parent == null)
                return false;
            _currentElement = parent;
            return true;
        }
        public override bool MoveToPrevious()
        {
            if (_attrIndex > -1)
                return false;

            var prev = _currentElement.GetPreviousElement();
            if (prev == null)
                return false;
            _currentElement = prev;
            return true;
        }

        public override bool MoveToId(string id)
        {
            return false;
        }
        public override bool MoveToFirstAttribute()
        {
            if (_attrIndex < 0)
            {
                _attrNames = _currentElement.GetAttributeNames();
                if (_attrNames == null)
                    return false;
                if (_attrNames.Length == 0)
                    return false;
            }
            _attrIndex = 0;
            return true;
        }
        public override bool MoveToNextAttribute()
        {
            if (_attrIndex == -1)
                return false;
            if (_attrIndex >= _attrNames.Length - 1)
                return false;
            _attrIndex++;
            return true;
        }

        public override string Name
        {
            get { return LocalName; }
        }
        public override XmlNameTable NameTable
        {
            get { return null; }
        }
        public override string NamespaceURI
        {
            get { return String.Empty; }
        }
        public override XPathNodeType NodeType
        {
            get
            {
                if (_attrIndex > -1)
                    return XPathNodeType.Attribute;
                return _currentElement.NodeType;
            }
        }
        public override string Prefix
        {
            get { return String.Empty; }
        }
        public override string Value
        {
            get
            {
                if (_attrIndex > -1)
                    return _currentElement.GetAttributeValue(_attrNames[_attrIndex]);
                return _currentElement.CollectTextValue();
            }
        }
        public override bool IsEmptyElement
        {
            get { return _currentElement.IsEmpty; }
        }
        public override string LocalName
        {
            get
            {
                if (_attrIndex > -1)
                    return _attrNames[_attrIndex];
                return _currentElement.Name;
            }
        }

        public override string GetAttribute(string localName, string namespaceURI)
        {
            return base.GetAttribute(localName, namespaceURI);
        }
        public override bool HasAttributes
        {
            get { return base.HasAttributes; }
        }

        public override string BaseURI
        {
            get { return String.Empty; }
        }
        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            return false;
        }
        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return false;
        }
    }
}

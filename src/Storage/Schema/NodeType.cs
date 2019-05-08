using System;
using System.Reflection;
using SenseNet.ContentRepository.Storage.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Schema
{
    public class NodeType : PropertySet
    {
        private static Type[] _newArgTypes = new Type[] { typeof(Node), typeof(string) };
        private static Type[] _loadArgTypes = new Type[] { typeof(NodeToken) };

        // ================================================================================ Fields

        private NodeType _parent;
        private TypeCollection<NodeType> _children;
        private TypeCollection<PropertyType> _declaredPropertyTypes;
        private string _nodeTypePath;

        private string _className;
        private Type _type;

        // ================================================================================ Properties

        public NodeType Parent
        {
            get { return _parent; }
        }
        public TypeCollection<NodeType> Children
        {
            get { return _children; }
        }
        public TypeCollection<PropertyType> DeclaredPropertyTypes
        {
            get { return _declaredPropertyTypes; }
        }
        public string NodeTypePath
        {
            get { return _nodeTypePath; }
        }
        public string ClassName
        {
            get { return _className; }
            internal set
            {
                if (_className != value)
                    _type = null;
                _className = value;
            }
        }

        // ================================================================================ Construction

        internal NodeType(int id, string name, ISchemaRoot schemaRoot, string className, NodeType parent)
            : base(id, name, schemaRoot)
        {
            _declaredPropertyTypes = new TypeCollection<PropertyType>(this.SchemaRoot);
            _parent = parent;
            _children = new TypeCollection<NodeType>(this.SchemaRoot);
            _className = className;
            _nodeTypePath = name;

            if (parent != null)
            {
                parent._children.Add(this);
                // Inherit PropertyTypes
                foreach (PropertyType propType in parent.PropertyTypes)
                    this.PropertyTypes.Add(propType);
                _nodeTypePath = String.Concat(parent._nodeTypePath, "/", _nodeTypePath);
            }
        }

        // ================================================================================ Methods

        public bool IsInstaceOfOrDerivedFrom(string nodeTypeName)
        {
            NodeType currentNodeType = this;
            while (currentNodeType != null)
            {
                if (currentNodeType.Name == nodeTypeName)
                    return true;
                currentNodeType = currentNodeType.Parent;
            }
            return false;
        }

        public bool IsInstaceOfOrDerivedFrom(NodeType nodeType)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");

            return IsInstaceOfOrDerivedFrom(nodeType.Name);
        }

        internal override void AddPropertyType(PropertyType propertyType)
        {
            if (propertyType.IsContentListProperty)
                throw new SchemaEditorCommandException(String.Concat("ContentListProperty cannot be assegned to a NodeType. NodeType=", this.Name, ", PropertyType=", propertyType.Name));
            if (!this.PropertyTypes.Contains(propertyType))
                this.PropertyTypes.Add(propertyType);
            if (!_declaredPropertyTypes.Contains(propertyType))
                _declaredPropertyTypes.Add(propertyType);
            InheritPropertyType(propertyType);
        }
        private void InheritPropertyType(PropertyType propertyType)
        {
            foreach (NodeType childSet in this.Children)
            {
                if (!childSet.PropertyTypes.Contains(propertyType))
                {
                    childSet.PropertyTypes.Add(propertyType);
                    childSet.InheritPropertyType(propertyType);
                }
            }
        }
        internal override void RemovePropertyType(PropertyType propertyType)
        {
            if (_parent != null && _parent.PropertyTypes.Contains(propertyType))
            {
                if (this.DeclaredPropertyTypes.Contains(propertyType))
                    this.DeclaredPropertyTypes.Remove(propertyType);
            }
            else
            {
                RemoveInheritedPropertyTypes(propertyType);
                this.PropertyTypes.Remove(propertyType);
                _declaredPropertyTypes.Remove(propertyType);
            }
        }
        private void RemoveInheritedPropertyTypes(PropertyType propertyType)
        {
            foreach (NodeType childSet in this.Children)
            {
                if (!childSet.DeclaredPropertyTypes.Contains(propertyType))
                {
                    childSet.RemoveInheritedPropertyTypes(propertyType);
                    childSet.PropertyTypes.Remove(propertyType);
                }
            }
        }

        private void UpdateNodeTypePath()
        {
            _nodeTypePath = _parent == null ? this.Name : String.Concat(_parent.NodeTypePath, "/", this.Name);
            foreach (NodeType nodeType in this.Children)
                nodeType.UpdateNodeTypePath();
        }

        public TypeCollection<NodeType> GetChildren()
        {
            TypeCollection<NodeType> children = new TypeCollection<NodeType>(this.SchemaRoot);
            foreach (NodeType nt in _children)
                children.Add(nt);
            return children;
        }

        public TypeCollection<NodeType> GetAllTypes()
        {
            TypeCollection<NodeType> types = new TypeCollection<NodeType>(this.SchemaRoot);
            this.GetAllTypes(types);
            return types;
        }
        private void GetAllTypes(TypeCollection<NodeType> types)
        {
            types.Add(this);
            foreach (NodeType subType in this.GetChildren())
                subType.GetAllTypes(types);
        }

        internal void MoveTo(NodeType parent)
        {
            if (this.Parent == parent)
                return;

            if (_parent != null)
                _parent.Children.Remove(this);
            _parent = parent;
            parent._children.Add(this);
            UpdateNodeTypePath();

            // Remove unwanted properties
            // #1 Unwanted properties are old inherited properties thats are own properties excepting the declared properties
            List<PropertyType> unwantedProps = new List<PropertyType>(this.PropertyTypes);
            foreach (PropertyType declaredProperty in this.DeclaredPropertyTypes)
                unwantedProps.Remove(declaredProperty);

            // #2 Remove old inherited properties excepting the new inherited properties
            foreach (PropertyType unwantedProp in unwantedProps)
                if (!_parent.PropertyTypes.Contains(unwantedProp))
                    RemovePropertyType(unwantedProp);

            // Inherit from new parent: add non-existent properties
            foreach (PropertyType newProp in _parent.PropertyTypes)
                if (!this.PropertyTypes.Contains(newProp))
                    AddPropertyType(newProp);
        }

        public static NodeType GetByName(string nodeTypeName)
        {
            return NodeTypeManager.Current.NodeTypes[nodeTypeName];
        }

        public static NodeType GetById(int nodeTypeId)
        {
            return NodeTypeManager.Current.NodeTypes.GetItemById(nodeTypeId);
        }

        // -------------------------------------------------------------------------------- Node Factory

        public static Node CreateInstance(string nodeTypeName, Node parent)
        {
            if (nodeTypeName == null)
                throw new ArgumentNullException("nodeTypeName");
            if (nodeTypeName.Length == 0)
                throw new ArgumentOutOfRangeException("nodeTypeName", "Argument cannot be empty");

            NodeType nodeType = NodeTypeManager.Current.NodeTypes[nodeTypeName];
            if (nodeType == null)
                throw new ApplicationException(String.Concat("NodeType not found: ", nodeTypeName));

            return nodeType.CreateInstance(parent);
        }
        public Node CreateInstance(Node parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (_type == null)
                _type = TypeResolver.GetType(_className, false);

            if (_type == null)
            {
                var exceptionMessage = string.Concat("Type not found, therefore the node can't be created.",
                    "\nClass name: ", _className,
                    "\nNode type path: ", _nodeTypePath,
                    "\nParent class name: ", (_parent != null ? _parent._className : "Parent is null"), "\n");
                throw new ApplicationException(exceptionMessage);
            }

            // only public ctor is valid: public NodeDescendant(Node parent, string nodeTypeName)
            ConstructorInfo ctor = _type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, _newArgTypes, null);
            if (ctor == null)
                throw new TypeInitializationException(string.Concat("Constructor not found. Valid signature: ctor(Node, string).\nClassName: ", _className), null);

            Node node;
            try
            {
                node = (Node)ctor.Invoke(new object[] { parent, this.Name });
            }
            catch (Exception ex) // rethrow
            {
                throw new ApplicationException(String.Concat("Couldn't create an instance of type '", _className,
                    "'. The invoked constructor threw an exception of type ", ex.GetType().Name, " (it said '", ex.Message, "')."), ex);
            }

            return node;

        }
        internal Node CreateInstance(NodeToken token)
        {
            if (_type == null)
                _type = TypeResolver.GetType(_className, false);

            if (_type == null)
            {
                var exceptionMessage = string.Format(CultureInfo.InvariantCulture, "Type not found, therefore the node can't be created.\nClass name: {0}\nNode type path: {1}\nParent class name: {2}\n", _className, _nodeTypePath, (_parent != null ? _parent._className : "Parent type is null"));
                if (token != null)
                    exceptionMessage = string.Concat(exceptionMessage, string.Format(CultureInfo.InvariantCulture, "Token.NodeId: {0}\nToken.Path: {1}", token.NodeId, (token.NodeData != null ? token.NodeData.Path : "UNKNOWN (InnerInfo is not loaded)")));
                else
                    exceptionMessage = string.Concat(exceptionMessage, "The given token is null.");
                throw new ApplicationException(exceptionMessage);
            }

            ConstructorInfo ctor = _type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, _loadArgTypes, null);
            if (ctor == null) // only protected ctor is valid
                throw new TypeInitializationException(token.NodeType.ClassName, new Exception("Protected constructor not found with a NodeToken parameter"));

            Node node;
            try
            {
                node = (Node)ctor.Invoke(new object[] { token });
            }
            catch (Exception ex) // rethrow
            {
                if (token.NodeData != null)
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't create an instance of type \"{0}\" (Path: {4}, NodeId: {1}). The invoked constructor threw an exception of type {2} (it said \"{3}\")."
                        , this.Name, token.NodeId, ex.GetType().Name, ex.Message, token.NodeData.Path), ex);
                else
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't create an instance of type \"{0}\" (NodeId: {1}). The invoked constructor threw an exception of type {2} (it said \"{3}\")."
                        , this.Name, token.NodeId, ex.GetType().Name, ex.Message), ex);
            }

            return node;

        }

    }
}
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Tests
{
    internal class Accessor
    {
        protected object _target;
        protected Type _type;

        public Accessor(object target)
        {
            _target = target;
            _type = target.GetType();
        }

        protected object GetInternalValue(string name)
        {
            PropertyInfo info = _type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return info.GetValue(_target, null);
        }
        protected void SetInternalValue(string name, object value)
        {
            PropertyInfo info = _type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
            info.SetValue(_target, value, null);
        }
        protected object GetPublicValue(string name)
        {
            PropertyInfo info = _type.GetProperty(name);
            return info.GetValue(_target, null);
        }
        protected void SetPublicValue(string name, object value)
        {
            PropertyInfo info = _type.GetProperty(name);
            info.SetValue(_target, value, null);
        }
        protected object CallPrivateMethod(string name, params object[] parameters)
        {
            //MethodInfo info = null;
            //if (parameters.Length == 0)
            //{
            //    info = _type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            //}
            //else
            //{
            //    Type[] paramTypes = new Type[parameters.Length];
            //    for (int i = 0; i < parameters.Length; i++)
            //        paramTypes[i] = parameters[i].GetType();
            //    info = _type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null, paramTypes, null);
            //}
            //return info.Invoke(_target, parameters);

            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                paramTypes[i] = parameters[i].GetType();
            return CallPrivateMethod(name, paramTypes, parameters);
        }
        protected object CallPrivateMethod(string name, Type[] paramTypes, object[] parameters)
        {
            MethodInfo info = null;
            if (parameters.Length == 0)
                info = _type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            else
                info = _type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null, paramTypes, null);
            System.Diagnostics.Debug.Assert(info != null, "Private method not found: " + name);
            return info.Invoke(_target, parameters);
        }
        protected object CallPrivateStaticMethod(string name, Type[] paramTypes, params object[] parameters)
        {
            return CallPrivateStaticMethod(_type, name, paramTypes, parameters);
        }
        protected static object CallPrivateStaticMethod(Type targetType, string name, Type[] paramTypes, params object[] parameters)
        {
            //MethodInfo info = _type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            foreach (MethodInfo info in targetType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                ParameterInfo[] paramInfos = info.GetParameters();
                if (info.Name == name)
                {
                    if (paramInfos.Length == paramTypes.Length)
                    {
                        bool ok = true;
                        for (int i = 0; i < paramInfos.Length; i++)
                        {
                            //if (paramInfos[i].ParameterType != paramTypes[i])
                            //{
                            if (!paramInfos[i].ParameterType.IsAssignableFrom(paramTypes[i]))
                            {
                                ok = false;
                                break;
                            }
                            //}
                        }
                        if (ok)
                            return info.Invoke(null, parameters);
                    }
                }
            }
            throw new ApplicationException("Method not found in a test");
        }
        protected object GetPrivateField(string name)
        {
            System.Reflection.FieldInfo fi = null;
            Type type = _type;
            while (type != null && fi == null)
            {
                fi = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            return fi.GetValue(_target);
        }
        protected void SetPrivateField(string name, object value)
        {
            System.Reflection.FieldInfo fi = null;
            Type type = _type;
            while (type != null && fi == null)
            {
                fi = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            fi.SetValue(_target, value);
        }

    }

    internal class SchemaEditorAccessor : Accessor
    {
        public SchemaEditorAccessor(SchemaEditor target) : base(target) { }
        public void RegisterSchema(SchemaEditor origSchema, SchemaWriter writer)
        {
            //private void RegisterSchema(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter schemaWriter)
            CallPrivateStaticMethod("RegisterSchema", new Type[] { typeof(SchemaEditor), typeof(SchemaEditor), typeof(SchemaWriter) }, origSchema, _target, writer);
        }
    }
    internal class SchemaItemAccessor : Accessor
    {
        public SchemaItemAccessor(SchemaItem target) : base(target) { }
        public int Id
        {
            get { return ((SchemaItem)_target).Id; }
            set { SetPrivateField("_id", value); }
        }
    }

    internal class ContentTypeManagerAccessor : Accessor
    {
        public ContentTypeManagerAccessor(ContentTypeManager target) : base(target) { }
        public ContentType LoadOrCreateNew(string contentTypeDefinitionXml)
        {
            return (ContentType)CallPrivateStaticMethod("LoadOrCreateNew", new Type[] { typeof(string) }, contentTypeDefinitionXml);
        }
        public void ApplyChangesInEditor(ContentType settings, SchemaEditor editor)
        {
            base.CallPrivateStaticMethod("ApplyChangesInEditor", new Type[] { typeof(ContentType), typeof(SchemaEditor) }, settings, editor);
        }
        public NodeTypeRegistrationAccessor ParseAttributes(Type type)
        {
            object ntr = base.CallPrivateStaticMethod("ParseAttributes", new Type[] { typeof(Type) }, type);
            if (ntr == null)
                return null;
            NodeTypeRegistrationAccessor ntrAcc = new NodeTypeRegistrationAccessor(ntr);
            return ntrAcc;
        }
    }
    internal class NodeTypeRegistrationAccessor : Accessor
    {
        private List<PropertyTypeRegistrationAccessor> _propertyTypeRegistrations;

        public Type Type
        {
            get { return (Type)GetPublicValue("Type"); }
            set { SetPublicValue("Type", value); }
        }
        public string Name
        {
            get { return (string)GetPublicValue("Name"); }
        }
        public string ParentName
        {
            get { return (string)GetPublicValue("ParentName"); }
            set { SetPublicValue("ParentName", value); }
        }
        public List<PropertyTypeRegistrationAccessor> PropertyTypeRegistrations
        {
            get { return _propertyTypeRegistrations; }
        }

        public NodeTypeRegistrationAccessor(object target)
            : base(target)
        {
            _propertyTypeRegistrations = new List<PropertyTypeRegistrationAccessor>();
            object listobject = GetPublicValue("PropertyTypeRegistrations");
            IEnumerable list = listobject as IEnumerable;
            foreach (object item in list)
            {
                PropertyTypeRegistrationAccessor p = new PropertyTypeRegistrationAccessor(item);
                p.Parent = this;
                _propertyTypeRegistrations.Add(p);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("NTR: name="); sb.Append(this.Name).Append(", ParentName=").Append(this.ParentName ?? "[null]");
            sb.Append(", Type=").Append(this.Type.FullName ?? "[null]").Append("\r\n");
            foreach (PropertyTypeRegistrationAccessor ptrAcc in _propertyTypeRegistrations)
                sb.Append("\t").Append(ptrAcc).Append("\r\n");
            return sb.ToString();
        }
    }
    internal class PropertyTypeRegistrationAccessor : Accessor
    {
        private NodeTypeRegistrationAccessor _parent;

        public string Name
        {
            get { return (string)GetPublicValue("Name"); }
        }
        public RepositoryDataType DataType
        {
            get { return (RepositoryDataType)GetPublicValue("DataType"); }
            set { SetPublicValue("DataType", value); }
        }
        public bool IsDeclared
        {
            get { return (bool)GetPublicValue("IsDeclared"); }
            set { SetPublicValue("IsDeclared", value); }
        }
        public NodeTypeRegistrationAccessor Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public PropertyTypeRegistrationAccessor(object target) : base(target) { }

        public override string ToString()
        {
            return String.Concat("PTR: name=", this.Name, ", DataType=", this.DataType, ", IsDeclared=", this.IsDeclared);
        }
    }

}

using System;
using System.Linq;
using System.Reflection;
// ReSharper disable UnusedParameter.Local

// ReSharper disable once CheckNamespace
namespace SenseNet.Services.Wopi.Tests.Accessors
{
    public class ObjectAccessor
    {
        private Type _targetType;
        private BindingFlags _publicFlags = BindingFlags.Instance | BindingFlags.Public;
        private BindingFlags _privateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        public object Target { get; }

        public ObjectAccessor(object target)
        {
            Target = target;
            _targetType = Target.GetType();
        }
        public ObjectAccessor(Type type, params object[] arguments)
        {
            _targetType = type;
            var ctor = GetConstructorByParams(type, arguments);
            Target = ctor.Invoke(arguments);
        }
        public ObjectAccessor(Type type, Type[] parameterTypes, object[] arguments)
        {
            _targetType = type;
            var ctor = GetConstructorByTypes(type, parameterTypes);
            Target = ctor.Invoke(arguments);
        }

        /// <summary>
        /// NOT IMPLEMENTED YET.
        /// </summary>
        public ObjectAccessor(object target, string memberToAccess)
        {
            throw new NotImplementedException();
        }

        private ConstructorInfo GetConstructorByParams(Type type, object[] arguments)
        {
            var argTypes = arguments.Select(a => a.GetType()).ToArray();
            return GetConstructorByTypes(type, argTypes);
        }
        private ConstructorInfo GetConstructorByTypes(Type type, Type[] argTypes)
        {
            var ctor = type.GetConstructor(argTypes);
            if (ctor == null)
                throw new ApplicationException("Constructor not found.");
            return ctor;
        }

        public object GetField(string fieldName)
        {
            var field = GetFieldInfo(fieldName);
            return field.GetValue(Target);
        }
        public void SetField(string fieldName, object value)
        {
            var field = GetFieldInfo(fieldName);
            field.SetValue(Target, value);
        }
        private FieldInfo GetFieldInfo(string name, bool throwOnError = true)
        {
            var field = _targetType.GetField(name, _publicFlags) ?? _targetType.GetField(name, _privateFlags);
            if (field == null && throwOnError)
                throw new ApplicationException("Field not found: " + name);
            return field;
        }

        public object GetProperty(string propertyName)
        {
            var property = GetPropertyInfo(propertyName);
            var method = property.GetGetMethod(true) ?? property.GetGetMethod(false);
            return method.Invoke(Target, null);
        }
        public void SetProperty(string propertyName, object value)
        {
            var property = GetPropertyInfo(propertyName);
            var method = property.GetSetMethod(true) ?? property.GetSetMethod(false);
            method.Invoke(Target, new [] { value });
        }
        private PropertyInfo GetPropertyInfo(string name, bool throwOnError = true)
        {
            var property = _targetType.GetProperty(name, _publicFlags) ?? _targetType.GetProperty(name, _privateFlags);
            if (property == null && throwOnError)
                throw new ApplicationException("Property not found: " + name);
            return property;
        }

        public object GetFieldOrProperty(string memberName)
        {
            var field = GetFieldInfo(memberName, false);
            if (field != null)
                return field.GetValue(Target);

            var property = GetPropertyInfo(memberName, false);
            if (property == null)
                throw new ApplicationException("Field or property not found: " + memberName);

            var method = property.GetGetMethod(true) ?? property.GetGetMethod(false);
            if (method == null)
                throw new ApplicationException("The property does not have getter: " + memberName);

            return method.Invoke(Target, null);
        }
        public void SetFieldOrProperty(string memberName, object value)
        {
            var field = GetFieldInfo(memberName, false);
            if (field != null)
            {
                field.SetValue(Target, value);
                return;
            }

            var property = GetPropertyInfo(memberName, false);
            if (property == null)
                throw new ApplicationException("Field or property not found: " + memberName);

            var method = property.GetSetMethod(true) ?? property.GetSetMethod(false);
            if (method == null)
                throw new ApplicationException("The property does not have setter: " + memberName);

            method.Invoke(Target, new [] { value });
        }

        public object Invoke(string name, params object[] args)
        {
            var paramTypes = args.Select(x => x.GetType()).ToArray();
            return Invoke(name, paramTypes, args);
        }
        public object Invoke(string name, Type[] parameterTypes, object[] args)
        {
            var method = _targetType.GetMethod(name, _privateFlags, null, parameterTypes, null)
                ?? _targetType.GetMethod(name, _publicFlags, null, parameterTypes, null);
            if (method == null)
                throw new ApplicationException("Method not found: " + name);
            return method.Invoke(Target, args);
        }
        /// <summary>
        /// NOT IMPLEMENTED YET.
        /// </summary>
        public object Invoke(string name, Type[] parameterTypes, object[] args, Type[] typeArguments)
        {
            throw new NotImplementedException();
        }
    }
}

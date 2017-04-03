using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Tools.SnAdmin.testability
{
    public interface ITypeResolverWrapper
    {
        object CreateInstance(string typeName);
        object CreateInstance(string typeName, params object[] args);
        T CreateInstance<T>(string typeName) where T : new();
        T CreateInstance<T>(string typeName, params object[] args);
        Type FindTypeInAppDomain(string typeName, bool throwOnError = true);
        Assembly[] GetAssemblies();
        Type GetType(string typeName, bool throwOnError = true);
        Type[] GetTypesByBaseType(Type baseType);
        Type[] GetTypesByInterface(Type interfaceType);
        string[] LoadAssembliesFrom(string path);
    }

    public static class TypeResolverWrapper
    {
        public static ITypeResolverWrapper Instance { get; set; } = new BuiltInTypeResolverWrapper();
    }

    internal class BuiltInTypeResolverWrapper : ITypeResolverWrapper
    {
        public object CreateInstance(string typeName)
        {
            return TypeResolver.CreateInstance(typeName);
        }
        public object CreateInstance(string typeName, params object[] args)
        {
            return TypeResolver.CreateInstance(typeName, args);
        }
        public T CreateInstance<T>(string typeName) where T : new()
        {
            return TypeResolver.CreateInstance<T>(typeName);
        }
        public T CreateInstance<T>(string typeName, params object[] args)
        {
            return TypeResolver.CreateInstance<T>(typeName, args);
        }
        public Type FindTypeInAppDomain(string typeName, bool throwOnError = true)
        {
            return TypeResolver.FindTypeInAppDomain(typeName, throwOnError);
        }
        public Assembly[] GetAssemblies()
        {
            return TypeResolver.GetAssemblies();
        }
        public Type GetType(string typeName, bool throwOnError = true)
        {
            return TypeResolver.GetType(typeName, throwOnError);
        }
        public Type[] GetTypesByBaseType(Type baseType)
        {
            return TypeResolver.GetTypesByBaseType(baseType);
        }
        public Type[] GetTypesByInterface(Type interfaceType)
        {
            return TypeResolver.GetTypesByInterface(interfaceType);
        }
        public string[] LoadAssembliesFrom(string path)
        {
            return TypeResolver.LoadAssembliesFrom(path);
        }
    }
}

using System;
using System.Reflection;
using System.Text;
using SenseNet.Tools.SnAdmin.Testability;

namespace SnAdminRuntime.Tests.Implementations
{
    public class TestTypeResolverWrapper : ITypeResolverWrapper
    {
        public StringBuilder Log { get; } = new StringBuilder();

        public object CreateInstance(string typeName)
        {
            throw new NotImplementedException();
        }

        public object CreateInstance(string typeName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public T CreateInstance<T>(string typeName) where T : new()
        {
            throw new NotImplementedException();
        }

        public T CreateInstance<T>(string typeName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public Type FindTypeInAppDomain(string typeName, bool throwOnError = true)
        {
            throw new NotImplementedException();
        }

        public Assembly[] GetAssemblies()
        {
            throw new NotImplementedException();
        }

        public Type GetType(string typeName, bool throwOnError = true)
        {
            throw new NotImplementedException();
        }

        public Type[] GetTypesByBaseType(Type baseType)
        {
            throw new NotImplementedException();
        }

        public Type[] GetTypesByInterface(Type interfaceType)
        {
            throw new NotImplementedException();
        }

        public string[] LoadAssembliesFrom(string path)
        {
            Log.AppendLine($"CALL: LoadAssembliesFrom({path})");
            return new string[0];
        }
    }
}

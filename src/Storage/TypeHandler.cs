using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public class AssemblyInfo
    {
        public string Name { get; set; }
        public bool IsDynamic { get; set; }
        [JsonIgnore]
        public string CodeBase { get; set; }
        public string Version { get; set; }
    }

    public static class TypeHandler
    {
        [Obsolete("Use TypeResolver.CreateInstance<T>(string) instead", true)]
        public static T CreateInstance<T>(string typeName) where T : new()
        {
            return TypeResolver.CreateInstance<T>(typeName);
        }
        [Obsolete("Use TypeResolver.CreateInstance<T>(string, object[]) instead", true)]
        public static T CreateInstance<T>(string typeName, params object[] args)
		{
            return TypeResolver.CreateInstance<T>(typeName, args);
		}
        [Obsolete("Use TypeResolver.CreateInstance(string) instead", true)]
        public static object CreateInstance(string typeName)
        {
            return TypeResolver.CreateInstance(typeName);
        }
        [Obsolete("Use TypeResolver.CreateInstance(string, object[]) instead", true)]
        public static object CreateInstance(string typeName, params object[] args)
        {
            return TypeResolver.CreateInstance(typeName, args);
		}

        [Obsolete("Use TypeResolver.GetType(string) instead", true)]
        public static Type GetType(string typeName)
        {
            return TypeResolver.GetType(typeName);
        }
        [Obsolete("Use TypeResolver.FindTypeInAppDomain(string) instead", true)]
        internal static Type FindTypeInAppDomain(string typeName)
        {
            return TypeResolver.FindTypeInAppDomain(typeName);
        }
        [Obsolete("Use TypeResolver.FindTypeInAppDomain(string, bool) instead", true)]
        internal static Type FindTypeInAppDomain(string typeName, bool throwOnError)
        {
            return TypeResolver.FindTypeInAppDomain(typeName, throwOnError);
        }

        [Obsolete("Use TypeResolver.GetAssemblies() instead", true)]
        public static Assembly[] GetAssemblies()
        {
            return TypeResolver.GetAssemblies();
        }

        [Obsolete("Use TypeResolver.LoadAssembliesFrom(string) instead", true)]
        public static string[] LoadAssembliesFrom(string path)
        {
            return TypeResolver.LoadAssembliesFrom(path);
		}

        [Obsolete("Use TypeResolver.GetTypesByInterface(Type) instead", true)]
        public static Type[] GetTypesByInterface(Type interfaceType)
        {
            return TypeResolver.GetTypesByInterface(interfaceType);
		}
        [Obsolete("Use TypeResolver.GetTypesByBaseType(Type) instead", true)]
        public static Type[] GetTypesByBaseType(Type baseType)
        {
            return TypeResolver.GetTypesByBaseType(baseType);
        }

        // ========================================================================= Oldschool container methods

        [Obsolete("Use Providers.Instance.GetProvider<T>(name) or any instrument of the SenseNet.Tools.TypeResolver.", true)]
        public static T ResolveProvider<T>() where T : class
        {
            return ResolveNamedType<T>(typeof(T).Name);
        }

        [Obsolete("Use Providers.Instance.GetProvider<T>(name) or any instrument of the SenseNet.Tools.TypeResolver.", true)]
        public static T ResolveNamedType<T>(string name) where T: class
        {
            return Providers.Instance.GetProvider<T>(name);
        }

        [Obsolete("Use Providers.Instance.GetProvider<T>(name) or any instrument of the SenseNet.Tools.TypeResolver.", true)]
        public static T ResolveInstance<T>(string name) where T : class
        {
            return ResolveNamedType<T>(name);
        }

        // =========================================================================

        /// <summary>
        /// Returns a sorted  list of 
        /// </summary>
        /// <returns></returns>
        public static AssemblyInfo[] GetAssemblyInfo()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => GetAssemblyInfo(a)).OrderBy(x => x.CodeBase).ToArray();
        }
        public static AssemblyInfo GetAssemblyInfo(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            return new AssemblyInfo
            {
                Name = asm.FullName,
                IsDynamic = asm.IsDynamic,
                CodeBase = GetCodeBase(asm),
                Version = GetAssemblyVersionString(asm)
            };
        }

        public static string GetCodeBase(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (asm.IsDynamic)
                return string.Empty;
            return asm.CodeBase.Replace("file:///", "").Replace("file://", "//").Replace("/", "\\");
        }

        public static Version GetVersion(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (asm.IsDynamic)
                return null;
            return asm.GetName().Version;
        }
        public static string GetAssemblyVersionString(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (asm.IsDynamic)
                return string.Empty;
            var ver = asm.GetName().Version.ToString();

            var dbg = IsDebugMode(asm);
            if (dbg.HasValue)
            {
                if (dbg.Value)
                    return ver + " Debug";
                return ver + " Release";
            }
            return ver;
        }

        public static bool? IsDebugMode(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (!asm.ReflectionOnly)
            {
                DebuggableAttribute dbgAttr;
                if ((dbgAttr = (DebuggableAttribute)(asm.GetCustomAttributes(typeof(DebuggableAttribute), false).FirstOrDefault())) != null)
                    return dbgAttr.IsJITTrackingEnabled;
                return false;
            }

            var data = asm.GetCustomAttributesData().FirstOrDefault(x => x.Constructor.ReflectedType == typeof(DebuggableAttribute));
            if (data == null)
                return false;
            if (data.ConstructorArguments.Count == 2)
                return (bool)(data.ConstructorArguments[0].Value);
            if (data.ConstructorArguments.Count == 1)
                return ((DebuggableAttribute.DebuggingModes)data.ConstructorArguments[0].Value & DebuggableAttribute.DebuggingModes.Default)
                    != DebuggableAttribute.DebuggingModes.None;
            return null;
        }
    }
}
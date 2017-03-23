using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Configuration;
using System.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage
{
    public class ApplicationInfo
    {
        public string Name { get; set; }
        public string AppId { get; set; }
        public Version Version { get; set; }
        public Version AcceptableVersion { get; set; }
        public string Description { get; set; }

        public static ApplicationInfo CreateInitialSenseNetVersion(Version version, string description)
        {
            return Data.DataProvider.Current.CreateInitialSenseNetVersion(version, description);
        }
    }

    public class AssemblyInfo
    {
        public string Name { get; set; }
        public bool IsDynamic { get; set; }
        public string CodeBase { get; set; }
        public string Version { get; set; }
    }

    public static class TypeHandler
    {
        [Obsolete("Use TypeResolver.CreateInstance<T>(string) instead")]
        public static T CreateInstance<T>(string typeName) where T : new()
        {
            return TypeResolver.CreateInstance<T>(typeName);
        }
        [Obsolete("Use TypeResolver.CreateInstance<T>(string, object[]) instead")]
        public static T CreateInstance<T>(string typeName, params object[] args)
		{
            return TypeResolver.CreateInstance<T>(typeName, args);
		}
        [Obsolete("Use TypeResolver.CreateInstance(string) instead")]
        public static object CreateInstance(string typeName)
        {
            return TypeResolver.CreateInstance(typeName);
        }
        [Obsolete("Use TypeResolver.CreateInstance(string, object[]) instead")]
        public static object CreateInstance(string typeName, params object[] args)
        {
            return TypeResolver.CreateInstance(typeName, args);
		}

        [Obsolete("Use TypeResolver.GetType(string) instead")]
        public static Type GetType(string typeName)
        {
            return TypeResolver.GetType(typeName);
        }
        [Obsolete("Use TypeResolver.FindTypeInAppDomain(string) instead")]
        internal static Type FindTypeInAppDomain(string typeName)
        {
            return TypeResolver.FindTypeInAppDomain(typeName);
        }
        [Obsolete("Use TypeResolver.FindTypeInAppDomain(string, bool) instead")]
        internal static Type FindTypeInAppDomain(string typeName, bool throwOnError)
        {
            return TypeResolver.FindTypeInAppDomain(typeName, throwOnError);
        }

        [Obsolete("Use TypeResolver.GetAssemblies() instead")]
        public static Assembly[] GetAssemblies()
        {
            return TypeResolver.GetAssemblies();
        }

        [Obsolete("Use TypeResolver.LoadAssembliesFrom(string) instead")]
        public static string[] LoadAssembliesFrom(string path)
        {
            return TypeResolver.LoadAssembliesFrom(path);
		}

        [Obsolete("Use TypeResolver.GetTypesByInterface(Type) instead")]
        public static Type[] GetTypesByInterface(Type interfaceType)
        {
            return TypeResolver.GetTypesByInterface(interfaceType);
		}
        [Obsolete("Use TypeResolver.GetTypesByBaseType(Type) instead")]
        public static Type[] GetTypesByBaseType(Type baseType)
        {
            return TypeResolver.GetTypesByBaseType(baseType);
        }

        // ========================================================================= Using Unity container

        public static T ResolveProvider<T>()
        {
            try
            {
                return Container.Resolve<T>();
            }
            catch (ResolutionFailedException)
            {
                return (T)(object)null;
            }
        }

        public static T ResolveNamedType<T>(string name)
        {
            try
            {
                return Container.Resolve<T>(name);
            }
            catch (ResolutionFailedException)
            {
                return (T)(object)null;
            }
        }

        public static T ResolveInstance<T>(string name)
        {
            try
            {
                return Container.Resolve<T>(name);
            }
            catch (ResolutionFailedException)
            {
                return (T)(object)null;
            }
        }

        private static UnityContainer _container;
        private static readonly object ContainerLock = new object();
        
        public static UnityContainer Container
        {
            get
            {
                if (_container == null)
                {
                    lock (ContainerLock)
                    {
                        if (_container == null)
                        {
                            _container = GetUnityContainer();
                        }
                    }
                }
                return _container;
            }
        }
        private static UnityContainer GetUnityContainer()
        {
            var container = new UnityContainer();
            var section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            if (section == null)
                throw new ConfigurationErrorsException("Unity section was not found. There is no configuration or it is invalid.");
            section.Configure(container, "Providers");

            return container;
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

        private static IDictionary<Type, Type[]> _configuredProviders;
        public static void Initialize(IDictionary<Type, Type[]> configuredProviders)
        {
            _configuredProviders = configuredProviders;
        }

        public static T GetProviderInstance<T>()
        {
            Type[] value;
            if (!_configuredProviders.TryGetValue(typeof(T), out value))
                return default(T);
            var type = value.FirstOrDefault();
            if (type == null)
                return default(T);
            return (T)Activator.CreateInstance(type);
        }
        public static Type[] GetProviderTypes()
        {
            return _configuredProviders.Keys.ToArray();
        }
        public static T[] GetProviderInstances<T>()
        {
            Type[] value;
            if (!_configuredProviders.TryGetValue(typeof(T), out value))
                return new T[0];
            if(value == null)
                return new T[0];
            var instances = value.Select(t => (T)Activator.CreateInstance(t)).ToArray();
            return instances;
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Configuration;
using System.Xml;
using SenseNet.Tools;

namespace SenseNet.Packaging.Steps
{
    /// <summary>Represents one activity in the package execution sequence</summary>
    public abstract class Step
    {
        public enum PathRelativeTo { Package, TargetDirectory }
        
        internal static Dictionary<string, Type> StepTypes { get; }

        static Step()
        {
            var stepTypes = new Dictionary<string,Type>();
            foreach (var stepType in TypeResolver.GetTypesByBaseType(typeof(Step)))
            {
                if (!stepType.IsAbstract)
                {
                    var step = (Step)Activator.CreateInstance(stepType);
                    stepTypes[step.ElementName] = stepType;
                    stepTypes[stepType.FullName] = stepType;
                }
            }
            StepTypes = stepTypes;
        }

        internal static Step Parse(XmlElement stepElement, int index, ExecutionContext executionContext)
        {
            var parameters = new Dictionary<string, XmlNode>();

            // attribute model
            foreach (XmlAttribute attr in stepElement.Attributes)
                parameters.Add(PackageManager.ToPascalCase(attr.Name), attr);

            var children = stepElement.SelectNodes("*");
            if (children.Count == 0 && stepElement.InnerXml != null && stepElement.InnerXml.Trim().Length > 0)
            {
                // default property model
                parameters.Add("", stepElement);
            }
            else
            {
                // element model
                foreach (XmlElement childElement in children)
                {
                    var name = childElement.Name;
                    if (parameters.ContainsKey(name))
                        throw new InvalidPackageException(String.Format(SR.Errors.StepParsing.AttributeAndElementNameCollision_2, stepElement.Name, name));
                    parameters.Add(name, childElement);
                }
            }

            return Step.BuildStep(index, stepElement.Name, parameters, executionContext);
        }

        internal static Step BuildStep(int stepId, string stepName, Dictionary<string, XmlNode> parameters, ExecutionContext executionContext)
        {
            Type stepType;
            if (!StepTypes.TryGetValue(stepName, out stepType))
                throw new InvalidPackageException(String.Format(SR.Errors.StepParsing.UnknownStep_1, stepName));

            var step = (Step)Activator.CreateInstance(stepType);
            step.StepId = stepId;
            foreach (var item in parameters)
            {
                if(item.Key.StartsWith("namespace-", StringComparison.OrdinalIgnoreCase))
                    step.AddXmlNamespace(item.Key.Substring("namespace-".Length), item.Value.Value);
                else
                    step.SetProperty(item.Key, item.Value, executionContext);
            }

            return step;
        }
        
        /*--------------------------------------------------------------------------------------------------------------------------------------------*/
        private void AddXmlNamespace(string alias, string xmlNamespace)
        {
            if(_xmlNamespaces == null)
                _xmlNamespaces = new Dictionary<string, string>();
            _xmlNamespaces[alias] = xmlNamespace;
        }

        internal void SetProperty(string name, string value)
        {
            SetProperty(GetProperty(name), value);
        }
        internal void SetProperty(string name, XmlNode value, ExecutionContext executionContext)
        {
            var prop = GetProperty(name);
            if (prop.PropertyType == typeof (IEnumerable<XmlElement>))
            {
                SetPropertyFromNestedElement(prop, value);
            }
            else
            {
                var stringValue = value is XmlAttribute ? value.Value : value.InnerXml;
                if (!string.IsNullOrEmpty(stringValue))
                {
                    var resolved = executionContext.ResolveVariable(stringValue);
                    if (resolved == null)
                    {
                        // Check if this is a package parameter that resolved to a null value. If yes, skip property set 
                        // to preserve the default value hardcoded in the step source code. If not, continue with setting
                        // the property with the variable name, because it may be a local variable used by the step itself
                        // (for example the ForEach step sets its own Item property as a variable unknown at this point).
                        if (executionContext.Manifest.Parameters.ContainsKey("@" + name))
                            return;
                    }
                    else
                    {
                        stringValue = resolved.ToString();
                    }
                }

                SetProperty(prop, stringValue);
            }
        }
        private void SetPropertyFromNestedElement(PropertyInfo prop, XmlNode value)
        {
            var element = value as XmlElement;
            if (element == null)
                return;

            try
            {
                var val = element.SelectNodes("*").Cast<XmlElement>().ToList();
                var setter = prop.GetSetMethod();
                setter.Invoke(this, new object[] { val });
            }
            catch (Exception e)
            {
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.CannotConvertToPropertyType_3, this.GetType().FullName, prop.Name, prop.PropertyType), e);
            }
        }
        private void SetProperty(PropertyInfo prop, string value)
        {

            if (!(prop.PropertyType.GetInterfaces().Any(x => x == typeof(IConvertible))))
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.PropertyTypeMustBeConvertible_2, this.GetType().FullName, prop.Name));

            var formatProvider = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                var val = prop.PropertyType.IsEnum
                        ? Enum.Parse(prop.PropertyType, value, true)
                        : ((IConvertible)(value)).ToType(prop.PropertyType, formatProvider);
                var setter = prop.GetSetMethod();
                setter.Invoke(this, new object[] { val });
            }
            catch (Exception e)
            {
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.CannotConvertToPropertyType_3, this.GetType().FullName, prop.Name, prop.PropertyType), e);
            }
        }
        private PropertyInfo GetProperty(string name)
        {
            var stepType = this.GetType();
            PropertyInfo prop = null;
            var propertyName = name;
            if (propertyName == string.Empty)
            {
                prop = GetDefaultProperty(stepType);
                if (prop == null)
                    throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.DefaultPropertyNotFound_1, stepType.FullName));
            }
            else
            {
                prop = stepType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null)
                    throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.UnknownProperty_2, stepType.FullName, propertyName));
            }
            return prop;
        }

        private static PropertyInfo GetDefaultProperty(Type stepType)
        {
            var props = stepType.GetProperties();
            foreach (var prop in props)
                if (prop.GetCustomAttributes(true).Any(x => x is DefaultPropertyAttribute))
                    return prop;
            return null;
        }

        /*============================================================================================================================================*/
        protected Dictionary<string, string> _xmlNamespaces;
        private Dictionary<XmlNameTable, XmlNamespaceManager> _nsmgrs;
        protected XmlNodeList SelectXmlNodes(XmlDocument doc, string xpath)
        {
            if (_xmlNamespaces == null)
                return doc.SelectNodes(xpath);

            if (_nsmgrs == null)
                _nsmgrs = new Dictionary<XmlNameTable, XmlNamespaceManager>();

            XmlNamespaceManager nsmgr;
            if (!_nsmgrs.TryGetValue(doc.NameTable, out nsmgr))
            {
                nsmgr = new XmlNamespaceManager(doc.NameTable);
                foreach (var item in _xmlNamespaces)
                    nsmgr.AddNamespace(item.Key, item.Value);
                _nsmgrs[doc.NameTable] = nsmgr;
            }

            return doc.SelectNodes(xpath, nsmgr);
        }

        #region =========================================================== Public instance part ===========================================================  
        /// <summary>Returns the XML name of the step element in the manifest. Default: simple or fully qualified name of the class.</summary>
        public virtual string ElementName { get { return this.GetType().Name; } }
        /// <summary>Order number in the phase.</summary>
        public int StepId { get; private set; }
        /// <summary>The method that executes the activity. Called by packaging framework.</summary>
        public abstract void Execute(ExecutionContext context);
        #endregion
        #region =========================================================== Common tools ===========================================================*/

        public enum TargetPathRelativeTo
        {
            WebRoot, Package, Sandbox, AppData
        }

        private static string GetTargetRoot(ExecutionContext context, TargetPathRelativeTo relativeTo)
        {
            string targetRoot = null;
            switch (relativeTo)
            {
                case TargetPathRelativeTo.WebRoot:
                    targetRoot = context.TargetPath;
                    break;
                case TargetPathRelativeTo.Package:
                    targetRoot = context.PackagePath;
                    break;
                case TargetPathRelativeTo.Sandbox:
                    targetRoot = context.SandboxPath;
                    break;
                case TargetPathRelativeTo.AppData:
                    targetRoot = Path.Combine(context.TargetPath, "App_Data");
                    break;
            }
            return targetRoot;
        }

        /// <summary>Returns a full path from repository if the path is absolute.</summary>
        protected static string ResolveRepositoryPath(string path, ExecutionContext context)
        {
            return ResolveVariable(path, context);
        }
        /// <summary>Returns with a full path under the package if the path is relative.</summary>
        protected static string ResolvePackagePath(string path, ExecutionContext context)
        {
            var contextPath = ResolveVariable(path, context);
            return ResolvePath(context.PackagePath, contextPath);
        }
        /// <summary>Returns with a full path under the target directory on the local server if the path is relative.</summary>
        protected static string ResolveTargetPath(string path, ExecutionContext context, TargetPathRelativeTo relativeTo = TargetPathRelativeTo.WebRoot)
        {
            var contextPath = ResolveVariable(path,context);
            return ResolvePath(GetTargetRoot(context, relativeTo), contextPath);
        }

        /// <summary>Converts a variable that declared earlier to its actual string value.</summary>
        public static string ResolveVariable(string variable, ExecutionContext context)
        {
            var resolvedVariable = (string)context.ResolveVariable(variable);
            if (string.IsNullOrWhiteSpace(resolvedVariable))
            {
                throw new InvalidStepParameterException("Invalid variable: " + variable);
            }
            return resolvedVariable;
        }

        public static void SetVariable(string name, object value, ExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("@"))
            {
                throw new InvalidStepParameterException("Invalid variable name.");
            }
            context.SetVariable(name, value);
        }

        private static string ResolvePath(string basePath, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;
            var path = Path.Combine(basePath, relativePath);
            var result = Path.GetFullPath(path);
            return result;
        }
        /// <summary>Returns with a full paths under the target directories on the network servers if the path is relative.</summary>
        protected static string[] ResolveNetworkTargets(string path, ExecutionContext context)
        {
            var contextPath = ResolveVariable(path, context);
            if (Path.IsPathRooted(contextPath))
                return new string[0];
            var resolved = context.NetworkTargets.Select(x => Path.GetFullPath(Path.Combine(x, contextPath))).ToArray();
            return resolved;
        }

        /// <summary>Returns a full path list under the target directories (on all servers if the path is relative).
        /// It is able to handle context variables (e.g. @Path) too.</summary>
        /// <param name="path">A relative file system path or a variable name (@Path).</param>
        /// <param name="context">Step execution context.</param>
        protected static string[] ResolveAllTargets(string path, ExecutionContext context)
        {
            var allTargets = new List<string>(context.NetworkTargets.Length + 1);
            allTargets.Add(ResolveTargetPath(path, context));
            allTargets.AddRange(ResolveNetworkTargets(path, context));
            return allTargets.ToArray();
        }
        #endregion
    }
}

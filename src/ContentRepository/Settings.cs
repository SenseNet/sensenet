﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Json;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Indexing;
using STT = System.Threading.Tasks;
// ReSharper disable ArrangeThisQualifier
// ReSharper disable RedundantBaseQualifier
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeStaticMemberQualifier
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// A Content handler base class for managing feature dependent, local or global settings 
    /// stored in the Content Repository instead of a config file in the file system.
    /// </summary>
    [ContentHandler]
    public partial class Settings : File, ISupportsDynamicFields, ISupportsAddingFieldsOnTheFly
    {
        /// <summary>
        /// This class serves as an internal helper for providing settings feature to
        /// the underlying Storage layer where the Settings class is unreachable.
        /// </summary>
        internal class SettingsManager : ISettingsManager
        {
            public T GetValue<T>(string settingsName, string key, string contextPath, T defaultValue)
            {
                return Settings.GetValue(settingsName, key, contextPath, defaultValue);
            }

            public bool IsSettingsAvailable(string settingsName, string contextPath = null)
            {
                return Settings.GetSettingsByName<Settings>(settingsName, contextPath) != null;
            }
        }

        internal static readonly string SETTINGSCONTAINERPATH = Repository.SettingsFolderPath; // "/Root/System/Settings";
        internal static readonly string SETTINGSCONTAINERNAME = Repository.SettingsFolderName; // "Settings";
        private static readonly string SETTINGSCONTAINERNAMEPART = "/" + SETTINGSCONTAINERNAME + "/";
        internal static readonly string EXTENSION = "settings";
        /// <summary>
        /// Defines a constant for the cache key of XML data.
        /// </summary>
        protected static readonly string BINARYXMLKEY = "CachedBinaryXml";
        /// <summary>
        /// Defines a constant for the cache key of JSON data.
        /// </summary>
        protected static readonly string BINARYJSONKEY = "CachedBinaryJson";
        /// <summary>
        /// Defines a constant for the cache key of interpreted data.
        /// </summary>
        protected static readonly string SETTINGVALUESKEY = "CachedValues";
        /// <summary>
        /// Defines a constant for the cache key of dynamic metadata.
        /// </summary>
        protected static readonly string DYNAMICMETADATA_CACHEKEY = "CachedDynamicMetadata";
        /// <summary>
        /// Defines a constant for the element name in the XML data.
        /// </summary>
        protected static readonly string XML_DEFAULT_NODE_NAME = "add";
        /// <summary>
        /// Defines a constant for the "key" attribute name in the XML data.
        /// </summary>
        protected static readonly string XML_DEFAULT_KEYATTRIBUTE_NAME = "key";
        /// <summary>
        /// Defines a constant for the "value" attribute name in the XML data.
        /// </summary>
        protected static readonly string XML_DEFAULT_VALUEATTRIBUTE_NAME = "value";

        // ================================================================================= Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Settings(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Settings(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected Settings(NodeToken nt) : base(nt) {}

        // ================================================================================= Properties

        private IDictionary<string, FieldMetadata> _dynamicFieldMetadata;
        private bool _dynamicFieldsChanged;

        /// <summary>
        /// Returns true if the setting binary is already loaded as an xml.
        /// </summary>
        protected bool XmlIsLoaded { get; set; } 
        private XmlDocument _binaryAsXml;
        /// <summary>
        /// Gets data as an <see cref="XmlDocument"/> if it can be parsed, or null.
        /// </summary>
        protected XmlDocument BinaryAsXml
        {
            get
            {
                if (_binaryAsXml == null && !XmlIsLoaded)
                {
                    if (this.Binary.Size > 0)
                    {
                        try
                        {
                            string binaryText;
                            using (var reader = new StreamReader(this.Binary.GetStream()))
                            {
                                binaryText = reader.ReadToEnd();
                            }

                            // check if it is really an xml
                            if (!string.IsNullOrEmpty(binaryText) && binaryText.StartsWith("<"))
                            {
                                var binXml = new XmlDocument();
                                binXml.Load(this.Binary.GetStream());

                                _binaryAsXml = binXml;
                                _binaryAsJObject = null;

                                base.SetCachedData(BINARYXMLKEY, _binaryAsXml);
                            }
                        }
                        catch (Exception ex)
                        {
                            SnLog.WriteWarning(string.Concat("Error during deserializing setting binary to an XML document. Path: ", this.Path, " ", ex));
                        }
                    }

                    XmlIsLoaded = true;
                }

                return _binaryAsXml;
            }
        }

        private bool _jsonIsLoaded;
        private JObject _binaryAsJObject;
        /// <summary>
        /// Gets data as a <see cref="JObject"/> if it can be parsed, or null.
        /// </summary>
        protected JObject BinaryAsJObject
        {
            get
            {
                if (_binaryAsJObject == null && !_jsonIsLoaded)
                {
                    if (this.Binary.Size > 0)
                        _binaryAsJObject = DeserializeToJObject(this.Binary.GetStream());

                    // If the settings object is otherwise empty, just initialize it with an empty JSON object
                    if (_binaryAsJObject == null)
                    {
                        _binaryAsJObject = new JObject();
                    }
                    else
                    {
                        _binaryAsXml = null;
                        base.SetCachedData(BINARYJSONKEY, _binaryAsJObject);
                    }

                    _jsonIsLoaded = true;
                }

                return _binaryAsJObject;
            }
        }

        private static readonly object _settingValuesLock = new object();
        private Dictionary<string, object> _settingValues;

        /// <summary>
        /// Gets the interpreted values as a Dictionary&lt;string, object&gt; instance.
        /// This property holds the real values for settings that were successfuly parsed from the binary. 
        /// This is stored in the node cache.
        /// </summary>
        protected Dictionary<string, object> SettingValues
        {
            get
            {
                if (_settingValues == null)
                {
                    lock (_settingValuesLock)
                    {
                        if (_settingValues == null)
                        {
                            // create an empty dictionary and place it into the cache
                            var localsettingValues = new Dictionary<string, object>();
                            base.SetCachedData(SETTINGVALUESKEY, localsettingValues);

                            _settingValues = localsettingValues;
                        }
                    }
                }

                return _settingValues;
            }
        }

        // ================================================================================= Settings API (STATIC)

        /// <summary>
        /// Loads a settings content with a specified name (or relative path) from the Settings folder.
        /// </summary>
        /// <typeparam name="T">The settings type.</typeparam>
        /// <param name="settingsName">Name or relative path of the settings content.</param>
        /// <param name="contextPath">The content where the search for the setting will start.</param>
        /// <returns>Strongly typed settings content or null.</returns>
        public static T GetSettingsByName<T>(string settingsName, string contextPath) where T : Settings
        {
            if (string.IsNullOrEmpty(settingsName))
                throw new ArgumentNullException(nameof(settingsName));

            try
            {
                return SettingsCache.GetSettingsByName<T>(settingsName, contextPath)
                       ?? Node.Load<T>(RepositoryPath.Combine(SETTINGSCONTAINERPATH, settingsName + "." + EXTENSION));
            }
            catch (Exception ex)
            {
                SnTrace.System.WriteError($"Error loading setting: {settingsName}. {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads all settings content with a specified name (or relative path) from the Settings folders 
        /// up on the parent chain, starting from the provided context path.
        /// </summary>
        /// <typeparam name="T">The settings type.</typeparam>
        /// <param name="settingsName">Name or relative path of the settings content.</param>
        /// <param name="contextPath">The content where the search for the settings will start.</param>
        /// <returns>List of strongly typed settings content.</returns>
        public static IEnumerable<T> GetAllSettingsByName<T>(string settingsName, string contextPath) where T : Settings
        {
            var settingsList = new List<T>();
            T setting;

            do
            {
                setting = GetSettingsByName<T>(settingsName, contextPath);
                if (setting == null) 
                    continue;

                settingsList.Add(setting);

                // if this is a local setting, try to find the value upwards
                if (!setting.Path.StartsWith(SETTINGSCONTAINERPATH))
                {
                    // find the path above the settings folder
                    contextPath = RepositoryPath.GetParentPath(GetParentContextPath(setting.Path));
                }
                else
                {
                    // found the global setting, skip out of the loop
                    break;
                }
            } while (setting != null);

            return settingsList;
        }

        /// <summary>
        /// Returns the input object converted to the desired type. If the input was null, the defaultValue will be returned.
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="value">Input raw value</param>
        /// <param name="defaultValue">Actual value if the "value" is null.</param>
        /// <returns></returns>
        protected static T ConvertSettingValue<T>(object value, T defaultValue)
        {
            if (value == null)
                return defaultValue;
            else if (value is T)
                return (T)value;
            else if (typeof(T).IsEnum && value is string)
                return (T)Enum.Parse(typeof(T), (string)value, true);
            else
                return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Returns a setting value by the given key of the specified <see cref="Settings"/>.
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="settingsName">Name of the <see cref="Settings"/> (e.g. Indexing or Portal).</param>
        /// <param name="key">The name of the requested value.</param>
        /// <param name="contextPath">The content where the search for the settings will start.</param>
        /// <param name="defaultValue">Value if the "value" is null.</param>
        public static T GetValue<T>(string settingsName, string key, string contextPath = null, T defaultValue = default(T))
        {
            using (new SystemAccount())
            {
                // load the settings file

                var settingsFile = GetSettingsByName<Settings>(settingsName, contextPath);

                // file not found, even in the global folder
                if (settingsFile == null)
                {
                    SnLog.WriteWarning("Settings file not found: " + settingsName + "." + EXTENSION);
                    return defaultValue;
                }

                // Try to get setting value from cache
                if (settingsFile.SettingValues.TryGetValue(key, out var settingValue))
                    return ConvertSettingValue<T>(settingValue, defaultValue);

                // Load the value from the Binary (xml or json format): this method should return a value 
                // that is already converted to type 'T' from string, otherwise the received default value.
                settingValue = settingsFile.GetValueFromBinary(key, defaultValue, out var found);

                // the value was found on the settings file
                if (found)
                {
                    settingValue = ConvertSettingValue<T>(settingValue, defaultValue);
                    settingsFile.AddValueToCache(key, settingValue);
                    return (T)settingValue;
                }

                // load the value from a content field if possible
                var settingsContent = Content.Create(settingsFile);
                if (settingsContent.Fields.ContainsKey(key))
                {
                    // NOTE: no need to add to cache here, we suppose that the content fields are already in the memory
                    //       (also, the dynamic fields of Settings are added to the cache in GetProperty)

                    var fieldValue = settingsContent[key];
                    if (fieldValue != null)
                    {
                        settingValue = ConvertSettingValue<T>(fieldValue, defaultValue);
                        return (T) settingValue;
                    }
                }

                // if this is a local setting, try to find the value upwards
                if (!settingsFile.Path.StartsWith(SETTINGSCONTAINERPATH))
                {
                    // find the path above the settings folder
                    var newPath = RepositoryPath.GetParentPath(GetParentContextPath(settingsFile.Path));
                    return GetValue(settingsName, key, newPath, defaultValue);
                }

                return defaultValue;
            }
        }

        /// <summary>
        /// Returns the closest <see cref="Settings"/> on the parent chain with the same name.
        /// </summary>
        /// <remarks>Note that the <see cref="Settings"/> is a context object with a special container.
        /// Every Content can have settings under it's "/Settings/[settingname]" structure.
        /// Searching the parent <see cref="Settings"/> is based on this structure.</remarks>
        protected Settings FindClosestInheritedSettingsFile()
        {
            if (this.ParentPath.StartsWith(SETTINGSCONTAINERPATH, true, System.Globalization.CultureInfo.InvariantCulture))
                return null;

            var newPath = RepositoryPath.GetParentPath(GetParentContextPath(this.Path));
            if (newPath == null)
                return null;

            var result = GetSettingsByName<Settings>(this.GetSettingName(), newPath);
            return result;
        }

        private static readonly JsonMergeSettings MergeControl = new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Ignore,
            PropertyNameComparison = StringComparison.InvariantCultureIgnoreCase
        };
        public static JObject GetEffectiveValues(string settingsName, string contextPath)
        {
            var allSettings = GetAllSettingsByName<Settings>(settingsName, contextPath).ToList();
            if (allSettings.Count == 0)
                return null;

            var effectiveValues = (JObject)allSettings.Last().BinaryAsJObject.DeepClone();
            if (allSettings.Count > 1)
            {
                allSettings.Reverse();
                for (var i = 1; i < allSettings.Count; i++)
                    effectiveValues.Merge(allSettings[i].BinaryAsJObject, MergeControl);
            }

            return effectiveValues;
        }

        // ================================================================================= Settings API (INSTANCE)

        /// <summary>
        /// Gets the setting value by name from the Binary field. The base implementation is able
        /// to work with XML and JSON format. Derived classes may understand other formats too.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="key">The name of the setting.</param>
        /// <param name="defaultValue">Default value if no data found in the binary.</param>
        /// <param name="found">Whether the value was found in the binary. If this is false, defaultValue should be returned.</param>
        /// <returns>The value found in the binary.</returns>
        protected virtual T GetValueFromBinary<T>(string key, T defaultValue, out bool found)
        {
            var xDoc = this.BinaryAsXml;
            if (xDoc?.DocumentElement != null)
            {
                // look for an xml node with a given name or a generic key/value 
                // node in the well-known '<add key value>' format
                var node = xDoc.DocumentElement.SelectSingleNode(key);
                if (node == null)
                {
                    var xpath = $"{XML_DEFAULT_NODE_NAME}[@{XML_DEFAULT_KEYATTRIBUTE_NAME} = \"{key}\"]";
                    var nodes = xDoc.DocumentElement.SelectNodes(xpath);
                    if (nodes != null && nodes.Count > 0)
                        node = nodes[0];
                }

                if (node != null)
                {
                    found = true;
                    return this.GetValueFromXmlInternal<T>(node, key);
                }
            }

            // try json format
            // only return the result if the key was found in the JSON text
            var jsonToken = BinaryAsJObject?[key];
            if (jsonToken != null && jsonToken.Type != JTokenType.Null)
            {
                found = true;
                return this.GetValueFromJsonInternal<T>(jsonToken, key);
            }

            // value was not found in the binary
            found = false;

            return defaultValue;
        }

        /// <summary>
        /// Gets the settings value from the found xml node.
        /// </summary>
        /// <param name="xmlNode">The xml node for the setting in the binary field.</param>
        /// <param name="key">The name of the setting.</param>
        /// <returns>The value of the setting. It should be of the requested setting type or null.</returns>
        protected virtual object GetValueFromXml(XmlNode xmlNode, string key)
        {
            //TODO: implement composite xml parsing (NameValueCollection)
            if (xmlNode.HasChildNodes && !(xmlNode.FirstChild is XmlText))
                throw new SnNotSupportedException("Composite setting value parsing is not supported.");

            return null;
        }

        /// <summary>
        /// Gets the settings value from the found JSON property.
        /// </summary>
        /// <param name="token">The JSON value for the setting in the binary field.</param>
        /// <param name="key">The name of the setting.</param>
        /// <returns>The value of the setting. It should be of the requested setting type or null.</returns>
        protected virtual object GetValueFromJson(JToken token, string key)
        {
            return null;
        }

        /// <summary>
        /// Returns deserialized JObject or null, if the deserializing was unsuccessful.
        /// </summary>
        /// <param name="stream">The input <see cref="Stream"/>.</param>
        public static JObject DeserializeToJObject(Stream stream)
        {
            JObject joe = null;

            try
            {
                string binaryText;
                using (var reader = new StreamReader(stream))
                {
                    binaryText = reader.ReadToEnd();
                }

                // check if it is not an xml
                if (!string.IsNullOrEmpty(binaryText) && !binaryText.StartsWith("<"))
                {
                    JToken deserialized;
                    using (var jreader = new JsonTextReader(new StringReader(binaryText)))
                    {
                        deserialized = JToken.ReadFrom(jreader);
                    }

                    if (deserialized is JObject o)
                        joe = o;
                    else if (deserialized is JArray && ((JArray)deserialized).Count > 0)
                        joe = ((JArray)deserialized)[0] as JObject;
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Error during deserializing setting binary to a JSON object. Error: " + ex.Message);
            }

            return joe;
        }

        // ================================================================================= Overrides

        /// <inheritdoc />
        /// <remarks>Looks for dynamic properties by the given name. If it was not found, continues the search 
        /// in the appropriate setting files on the parent chain.</remarks>
        public override object GetProperty(string name)
        {
            if (this.HasProperty(name))
                return base.GetProperty(name);

            if (BinaryAsJObject != null)
            {
                object result = JsonDynamicFieldHelper.GetProperty(BinaryAsJObject, name, out var found);

                // If not found, try the inherited settings file
                if (!found)
                {
                    var inherited = this.FindClosestInheritedSettingsFile();
                    if (inherited != null)
                        return inherited.GetProperty(name);
                }
                else
                {
                    AddValueToCache(name, result);
                }

                return result;
            }
            else
                return null;
        }

        /// <inheritdoc />
        /// <remarks>Sets or overrides any dynamic or well-known property.</remarks>
        public override void SetProperty(string name, object value)
        {
            if (this.HasProperty(name))
            {
                base.SetProperty(name, value);
            }
            else if (BinaryAsJObject != null)
            {
                // If the value is the same as what the inherited settings contains, set it to null, thus removing it from the JSON
                var inherited = this.FindClosestInheritedSettingsFile();
                var inheritedValue = inherited?.GetProperty(name);
                if (inheritedValue != null && inheritedValue.Equals(value))
                    value = null;

                JsonDynamicFieldHelper.SetProperty(BinaryAsJObject, name, value);
                _dynamicFieldsChanged = true;
            }
        }

        [Obsolete("Use async version instead.", true)]
        public override void Save(NodeSaveSettings settings)
        {
            SaveAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
        }
        public override async System.Threading.Tasks.Task SaveAsync(NodeSaveSettings settings, CancellationToken cancel)
        {
            AssertSettings();

            if (_dynamicFieldsChanged && BinaryAsJObject != null)
            {
                // If this is a JSON settings file and the dynamic metadata changed, save the JSON binary according to the changes
                await JsonDynamicFieldHelper.SaveToStreamAsync(BinaryAsJObject, async (stream, c) =>
                {
                    this.Binary.SetStream(stream);
                    await base.SaveAsync(settings, c);
                    _dynamicFieldsChanged = false;
                }, cancel);
            }
            else
            {
                await base.SaveAsync(settings, cancel).ConfigureAwait(false);
            }

            // Remove all items from cache on the parent settings chain
            var allSettings = GetAllSettingsByName<Settings>(Name.Replace(".settings", ""), Path).ToList();
            foreach (var item in allSettings)
            {
                item._jsonIsLoaded = false;
                item._binaryAsJObject = null;
                PathDependency.FireChanged(item.Path);
                NodeIdDependency.FireChanged(item.Id);
            }

            // Find all settings that inherit from this setting and remove their cached data.
            if (RepositoryInstance.IndexingEngineIsRunning && !RepositoryEnvironment.WorkingMode.Importing)
            {
                var contextPath = this.ParentPath.StartsWith(SETTINGSCONTAINERPATH, true,
                    System.Globalization.CultureInfo.InvariantCulture)
                    ? Identifiers.RootPath
                    : GetParentContextPath(this.Path);

                if (contextPath != null)
                {
                    using (new SystemAccount())
                    {
                        var q = await ContentQuery.QueryAsync(SafeQueries.InTreeAndTypeIsAndName,
                                new QuerySettings { EnableAutofilters = FilterStatus.Disabled }, cancel,
                                contextPath, nameof(Settings), this.Name)
                            .ConfigureAwait(false);

                        foreach (var id in q.Identifiers)
                            NodeIdDependency.FireChanged(id);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void MoveTo(Node target)
        {
            AssertSettingsPath(RepositoryPath.Combine(target.Path, this.Name), this.Name, this.Id);
            base.MoveTo(target);
        }

        /// <summary>
        /// Overrides the base class behavior. Triggers the building of internal structures.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            // load cached xml if exists
            _binaryAsXml = (XmlDocument)base.GetCachedData(BINARYXMLKEY);
            if (_binaryAsXml != null)
                XmlIsLoaded = true;

            // load cached json if exists
            _binaryAsJObject = (JObject)base.GetCachedData(BINARYJSONKEY);
            if (_binaryAsJObject != null)
                _jsonIsLoaded = true;

            // load cached values if possible
            _settingValues = (Dictionary<string, object>) base.GetCachedData(SETTINGVALUESKEY);
        }

        // ================================================================================= Helper methods

        private void AssertSettings()
        {
            AssertSettingsPath(RepositoryPath.Combine(this.ParentPath, this.Name), this.Name, this.Id);
        }
        private void AssertSettingsPath(string path, string name, int id)
        {
            if (!path.Contains(SETTINGSCONTAINERNAMEPART) &&
                !path.StartsWith(RepositoryPath.Combine(SETTINGSCONTAINERPATH, RepositoryPath.PathSeparator), StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith(RepositoryPath.Combine(RepositoryStructure.ContentTemplateFolderPath, RepositoryPath.PathSeparator), StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith(RepositoryPath.Combine(TrashBin.TrashBinPath, RepositoryPath.PathSeparator), StringComparison.OrdinalIgnoreCase)
                )
                throw new InvalidContentException(String.Format(SR.GetString(SR.Exceptions.Settings.Error_ForbiddenPath_2), SETTINGSCONTAINERNAMEPART, SETTINGSCONTAINERPATH));

            // check extension
            if (!name.EndsWith("." + EXTENSION))
                throw new InvalidContentException(String.Format(SR.GetString(SR.Exceptions.Settings.Error_ForbiddenExtension), name));

            // Check name 
            // 1. settings content in the settings folder with the same name
            // 2. global only settings: cannot create a local setting if a global-only setting exists with the same name
            var p = path.IndexOf(SETTINGSCONTAINERNAMEPART, StringComparison.Ordinal);
            var rootpath = p >= 0 ? path.Substring(0, p + SETTINGSCONTAINERNAMEPART.Length - 1) : SETTINGSCONTAINERPATH;
            
            var nameError = false;
            var globalSettingError = false;
            if (Providers.Instance.SearchManager.ContentQueryIsAllowed)
            {
                if (ContentQuery.QueryAsync(SafeQueries.SettingsByNameAndSubtree, null, CancellationToken.None, name, id, rootpath)
                        .ConfigureAwait(false).GetAwaiter().GetResult().Count > 0)
                    nameError = true;

                // check global settings only if this is a local setting
                if (!nameError &&
                    !path.StartsWith(SETTINGSCONTAINERPATH + RepositoryPath.PathSeparator, StringComparison.InvariantCulture) &&
                    ContentQuery.QueryAsync(SafeQueries.SettingsGlobalOnly, null, CancellationToken.None, name, SETTINGSCONTAINERPATH)
                        .ConfigureAwait(false).GetAwaiter().GetResult().Count > 0)
                    globalSettingError = true;
            }
            else
            {
                var settingsType = Providers.Instance.StorageSchema.NodeTypes["Settings"];

                // query content without outer search engine
                var nqResult = NodeQuery.QueryNodesByTypeAndPathAndName(settingsType, false, rootpath, false, name);
                if (nqResult.Nodes.Any(n => n.Id != id))
                    nameError = true;

                // check global settings only if this is a local setting
                if (!nameError && !path.StartsWith(SETTINGSCONTAINERPATH + RepositoryPath.PathSeparator, StringComparison.InvariantCulture))
                {
                    // Load all global-only settings, look for the same name in memory (because 
                    // settings may be organized into a subtree under the global folder!)
                    nqResult = NodeQuery.QueryNodesByTypeAndPathAndProperty(settingsType, false,
                        SETTINGSCONTAINERPATH, false, new[]
                        {
                            new QueryPropertyData
                            {
                                PropertyName = "GlobalOnly",
                                QueryOperator = Operator.Equal,
                                Value = 1
                            }
                        }.ToList());

                    if (nqResult.Nodes.Any(n => string.CompareOrdinal(n.Name, name) == 0))
                        globalSettingError = true;
                }
            }
            if (nameError)
                throw new InvalidContentException(string.Concat(SR.GetString(SR.Exceptions.Settings.Error_NameExists_1), " Name: ", this.Name));
            if (globalSettingError)
                throw new InvalidContentException(string.Format(SR.GetString(SR.Exceptions.Settings.Error_GlobalOnly_1), name));
        }

        private void AddValueToCache(string key, object value)
        {
            lock (_settingValuesLock)
            {
                this.SettingValues[key] = value;
            }
        }

        private T GetValueFromXmlInternal<T>(XmlNode xmlNode, string key)
        {
            // Get the value from the virtual method that returns null in the default implementation.
            // If a custom inheritor implements this method, it can override the default conversion 
            // behavior implemented below.
            var convertedValue = GetValueFromXml(xmlNode, key);
            if (convertedValue != null)
                return (T) convertedValue;

            // get the string value from the xml node
            var stringValue = GetInnerTextOrAttribute(xmlNode);
            if (string.IsNullOrEmpty(stringValue))
                return default(T);

            object returnValue;
            var tt = typeof(T);

            // default type conversions
            if (tt == typeof(string))
                returnValue = stringValue;
            else if (tt == typeof(int))
                returnValue = Convert.ToInt32(stringValue);
            else if (tt == typeof(bool))
                returnValue = Convert.ToBoolean(stringValue);
            else if (tt == typeof(DateTime))
                returnValue = Convert.ToDateTime(stringValue);
            else if (tt == typeof(decimal))
                returnValue = Convert.ToDecimal(stringValue);
            else if (tt == typeof(float))
                returnValue = float.Parse(stringValue);
            else if (tt == typeof(double))
                returnValue = Convert.ToDouble(stringValue);
            else if (tt == typeof(long))
                returnValue = Convert.ToInt64(stringValue);
            else if (tt == typeof(string[]) || tt == typeof(IEnumerable<string>))
                returnValue = stringValue.Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);
            else if (tt.IsEnum)
            {
                try
                {
                    returnValue = Enum.Parse(tt, stringValue, true);
                }
                catch
                {
                    SnLog.WriteWarning("Unknown value for enum type. Value: " + stringValue + ", Type: " + tt.FullName + ". Settings: " + this.Path);
                    throw new InvalidContentException(String.Format(SR.GetString(SR.Exceptions.Settings.Error_InvalidEnumValue_2), stringValue, tt.FullName));
                }
            }
            else
                throw new SnNotSupportedException("Not supported settings type: " + tt.Name);

            return (T)returnValue;
        }
        
        private T GetValueFromJsonInternal<T>(JToken token, string key)
        {
            if (token == null)
                return default(T);

            // Get the value from the virtual method that returns null in the default implementation.
            // If a custom inheritor implements this method, it can override the default conversion 
            // behavior implemented below.
            var convertedValue = GetValueFromJson(token, key);
            if (convertedValue != null)
                return (T)convertedValue;

            // get the value from the json token
            try
            {
                var tt = typeof(T);

                // check for Enum type
                if (tt.IsEnum)
                {
                    return (T)Enum.Parse(tt, token.Value<string>(), true);
                }

                // check for Array type
                if (token is JArray jArray)
                {
                    if (!typeof(IEnumerable).IsAssignableFrom(tt))
                        throw new InvalidOperationException($"Cannot convert a JArray to {tt.FullName}.");

                    return jArray.ToObject<T>();
                }

                // handle custom objects
                if (token is JObject)
                    return token.ToObject<T>();

                // any other type
                return token.Value<T>();
            }
            catch(Exception ex)
            {
                SnLog.WriteWarning(
                    $"Error during setting value JSON conversion. Path: {this.Path}. Key: {key}. Expected type: {typeof(T).FullName}. Exception: {ex.Message}");
            }

            return default(T);
        }

        /// <summary>
        /// Gets the string value from an xml node, either the inner text or the value of the 'value' attribute.
        /// </summary>
        /// <param name="xmlNode">An XML node.</param>
        /// <returns>If an attribute called 'value' exists, than its value. Otherwise the inner text of the xml node.</returns>
        protected string GetInnerTextOrAttribute(XmlNode xmlNode)
        {
            if(xmlNode == null)
                throw new ArgumentException(nameof(xmlNode));

            // ReSharper disable once PossibleNullReferenceException
            var attr = xmlNode.Attributes[XML_DEFAULT_VALUEATTRIBUTE_NAME];
            return attr != null ? attr.Value : xmlNode.InnerText;
        }

        /// <summary>
        /// Returns name of this instance without file name extension.
        /// </summary>
        protected string GetSettingName()
        {
            if (this.IsNew)
                return this.Name;

            var s = RepositoryPath.PathSeparator + SETTINGSCONTAINERNAME + RepositoryPath.PathSeparator;
            var l = this.Path.LastIndexOf(s, StringComparison.Ordinal);
            var result = this.Path.Substring(l + s.Length);
            result = result.Substring(0, result.LastIndexOf("." + EXTENSION, StringComparison.Ordinal));
            return result;
        }

        private static string GetParentContextPath(string settingsPath)
        {
            if (string.IsNullOrEmpty(settingsPath) || settingsPath.Equals("/root", StringComparison.OrdinalIgnoreCase))
                return null;

            var result = settingsPath.Substring(0, settingsPath
                .LastIndexOf(RepositoryPath.PathSeparator + SETTINGSCONTAINERNAME + RepositoryPath.PathSeparator, StringComparison.Ordinal));
            return result;
        }

        /// <summary>
        /// Overrides the base class behavior.
        /// In this case disables the indexing of the dynamic fields.
        /// Do not use this method directly from your code.
        /// </summary>
        public override IEnumerable<IIndexableField> GetIndexableFields()
        {
            // NOTE:
            // A Settings content can contain any user-defined JSON object. The properties of that JSON object will appear as dynamic fields on the Content layer.
            // The problem is that sensenet can't handle two differently typed fields with the same name.
            // However, the name of these fields may collide with the name of a CTD field or a name of another dynamic field.
            // ----------
            // For now, the solution is to disable indexing for the dynamic fields on Settings.

            var baseResult = base.GetIndexableFields();
            var result = new List<IIndexableField>();

            // Remove dynamic fields from indexable fields
            foreach (var item in baseResult)
            {
                if (this._dynamicFieldMetadata.All(m => m.Value.FieldName != item.Name) || this.HasProperty(item.Name))
                    result.Add(item);
            }

            return result;
        }


        // ================================================================================= ISupportsDynamicFields implementation

        IDictionary<string, FieldMetadata> ISupportsDynamicFields.GetDynamicFieldMetadata()
        {
            if (_dynamicFieldMetadata == null)
                BuildDynamicFieldMetadata();

            return _dynamicFieldMetadata;
        }

        void ISupportsDynamicFields.ResetDynamicFields()
        {
            // reset cached dynamic fields
            this.SetCachedData(DYNAMICMETADATA_CACHEKEY, null);
            _dynamicFieldMetadata = null;
            _settingValues = null;
        }

        bool ISupportsDynamicFields.IsNewContent => this.IsNew;

        private void BuildDynamicFieldMetadata()
        {
            if (this.GetCachedData(DYNAMICMETADATA_CACHEKEY) is IDictionary<string, FieldMetadata> cachedMetadata)
            {
                _dynamicFieldMetadata = cachedMetadata;
            }
            else
            {
                if (BinaryAsJObject != null)
                {
                    var meta = new Dictionary<string, FieldMetadata>();

                    if (!this.IsNew)
                    {
                        // Find inherited settings files
                        var chain = GetAllSettingsByName<Settings>(this.GetSettingName(), this.Path).ToList();
                        
                        // Workaround in case the current item was not yet loaded into the settings cache: the
                        // first element in this chain should always be the current settings file.
                        if (chain.Count == 0 || chain[0].Id != this.Id)
                            chain.Insert(0, this);

                        // Get metadata from inherited settings files
                        var fieldDictionaries = chain.Select(x => JsonDynamicFieldHelper.BuildDynamicFieldMetadata(x.BinaryAsJObject)).ToList();

                        // The result should be composed in a way that the inheritor's metadata always overrides the original metadata
                        fieldDictionaries.Reverse();
                        foreach (var dict in fieldDictionaries)
                            foreach (var k in dict.Keys)
                                meta[k] = dict[k];
                    }

                    _dynamicFieldMetadata = meta;
                }
                else
                {
                    _dynamicFieldMetadata = new Dictionary<string, FieldMetadata>();
                }

                this.SetCachedData(DYNAMICMETADATA_CACHEKEY, _dynamicFieldMetadata);
            }
        }

        // ================================================================================= ISupportsAddingFieldsOnTheFly implementation

        bool ISupportsAddingFieldsOnTheFly.AddFields(IEnumerable<FieldMetadata> fields)
        {
            if (BinaryAsJObject == null)
                return false;

            if (_dynamicFieldMetadata == null)
                BuildDynamicFieldMetadata();

            foreach (var field in fields)
            {
                _dynamicFieldMetadata.Add(field.FieldName, field);
                if (field.FieldSetting != null)
                    JsonDynamicFieldHelper.SetProperty(BinaryAsJObject, field.FieldName, field.FieldSetting.DefaultValue);
            }

            return true;
        }
    }    
}

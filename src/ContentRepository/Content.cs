using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using System.Linq;
using System.IO;
using SenseNet.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using SenseNet.ContentRepository.Fields;
using System.Web;
using System.Xml.XPath;
using System.Web.Configuration;
using SenseNet.ApplicationModel;
using SenseNet.Search;
using System.Collections;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Linq;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    public enum FieldSerializationOptions { All, None, Custom }
    public enum ActionSerializationOptions { All, None/*, BrowseOnly*/ }
    public class SerializationOptions
    {
        private class DefaultSerializationOptions : SerializationOptions
        {
            public override FieldSerializationOptions Fields { get { return FieldSerializationOptions.All; } set { } }
            public override IEnumerable<string> FieldNames { get { return null; } set { } }
            public override ActionSerializationOptions Actions { get { return ActionSerializationOptions.All; } set { } }
            public override string Language { get { return CultureInfo.CurrentUICulture.Name; } set { } }
        }
        public virtual FieldSerializationOptions Fields { get; set; }
        public virtual IEnumerable<string> FieldNames { get; set; }
        public virtual ActionSerializationOptions Actions { get; set; }

        private string _lang;
        public virtual string Language
        {
            get { return _lang ?? CultureInfo.CurrentUICulture.Name; }
            set { _lang = value; }
        }

        protected static readonly string UNKNOWN_SITE = "unknown";

        public virtual string GetHash()
        {
            var site = HttpContext.Current != null && HttpContext.Current.Request != null
                ? HttpContext.Current.Request.Url.Host
                : UNKNOWN_SITE;


            var keyText = string.Join("#",
                Fields,
                (FieldNames != null ? string.Join(",", FieldNames) : string.Empty),
                site,
                Actions,
                Language);

            var sha = new SHA1CryptoServiceProvider();
            var encoding = new UnicodeEncoding();
            return Convert.ToBase64String(sha.ComputeHash(encoding.GetBytes(keyText)));
        }

        public static readonly SerializationOptions _default = new DefaultSerializationOptions();
        public static SerializationOptions Default { get { return _default; } }
    }
    public interface IActionLinkResolver
    {
        string ResolveRelative(string targetPath);
        string ResolveRelative(string targetPath, string actionName);
    }
    /// <summary>
    /// <c>Content</c> class is responsible for the general management of different types of <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>s.
    /// </summary>
    /// <remarks>
    /// Through this class you can generally load, create, validate and save any kind 
    /// of <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandlers</see>.
    /// 
    /// The type of a Content is defined by the <see cref="SenseNet.ContentRepository.Schema.ContentType">ContentType</see>, 
    /// which represents the ContentTypeDefinition. The most important component of a <c>Content</c> 
    /// is <see cref="SenseNet.ContentRepository.Field">Field</see> list, which is defined also in ContentTypeDefinition. 
    /// 
    /// Basically a <c>Content</c> is a wrapper of the <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> assigned 
    /// when the <c>Content</c> itself was created, and the <see cref="SenseNet.ContentRepository.Field">Fields</see> are managing 
    /// the properties of the ContentHandler.
    /// </remarks>
    /// <example>
    /// 
    /// The following code shows a method that handles a <c>Content</c> without user interface:
    /// <code>
    /// // sets the Index field of an identified Content if possible
    /// public void SetIndex(int id, int expectedIndex)
    /// {
    ///     // load the content
    ///     var content = Content.Load(id);
    ///
    ///     // check the existence
    ///     if(content == null)
    ///          return;
    ///
    ///     int originalIndex = (int)content["Index"];
    ///
    ///     // avoid the unnecessary validation and saving
    ///     if(originalIndex == expectedIndex)
    ///         return;
    ///
    ///     // set the field
    ///     content["Index"] = expectedIndex;
    ///     
    ///     // check the validity
    ///     if (content.IsValid)
    ///     {
    ///         //TODO: exception handling if needed
    ///         content.Save();
    ///     }
    ///     else
    ///     {
    ///         //TODO: excepton throwing if index is invalid by current FieldSetting
    ///     }
    /// }
    /// </code>
    /// </example>
    public class Content : FeedContent, ICustomTypeDescriptor
    {
        public static class Operations
        {
            /// <summary>
            /// Rebuilds the Lucene index document of a content and optionally of all documents in the whole subtree. 
            /// In case the value of <value>rebuildLevel</value> is <value>IndexOnly</value> the index document is refreshed 
            /// based on the already existing extracted data stored in the database. This is a significantly faster method 
            /// and it is designed for cases when only the place of the content in the tree has changed or the index got corrupted.
            /// The <value>DatabaseAndIndex</value> algorithm will reindex the full content than update the Lucene index in the
            /// file system the same way as the light-weight algorithm.
            /// </summary>
            /// <param name="content">The content provided by the infrastructure.</param>
            /// <param name="recursive">Whether child content should be reindexed or not. Default: false.</param>
            /// <param name="rebuildLevel">The algorithm selector. Value can be <value>IndexOnly</value> or <value>DatabaseAndIndex</value>. Default: <value>IndexOnly</value></param>
            [ODataAction]
            public static void RebuildIndex(Content content, bool recursive, IndexRebuildLevel rebuildLevel)
            {
                content.RebuildIndex(recursive, rebuildLevel);
            }
            /// <summary>
            /// Performes a full reindex operation on the content and the whole subtree.
            /// </summary>
            /// <param name="content">The content provided by the infrastructure.</param>
            [ODataAction]
            public static void RebuildIndexSubtree(Content content)
            {
                content.RebuildIndex(true, IndexRebuildLevel.DatabaseAndIndex);
            }
            /// <summary>
            /// Refreshes the index document of the content and the whole subtree using the already existing index data stored in the database.
            /// </summary>
            /// <param name="content">The content provided by the infrastructure.</param>
            [ODataAction]
            public static void RefreshIndexSubtree(Content content)
            {
                content.RebuildIndex(true, IndexRebuildLevel.IndexOnly);
            }
        }

        public static bool ContentNavigatorEnabled => RepositoryEnvironment.XsltRenderingWithContentSerialization;

        // ========================================================================= Fields

        private ContentType _contentType;

        private Node _contentHandler;
        private IDictionary<string, Field> _fields;
        private IEnumerable<FieldSetting> _fieldSettings;
        private bool _isValidated;
        private bool _isValid;

        // ========================================================================= Properties

        /// <summary>
        /// Gets whether this content items supports dynamically adding fields to it on the fly
        /// </summary>
        public bool SupportsAddingFieldsOnTheFly
        {
            get { return this.ContentHandler is ISupportsAddingFieldsOnTheFly; }
        }

        /// <summary>
        /// Gets the <see cref="SenseNet.ContentRepository.Schema.ContentType">ContentType</see> of the instance.
        /// </summary>
        public ContentType ContentType
        {
            get { return _contentType; }
        }
        /// <summary>
        /// Gets the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> of the instance.
        /// </summary>
        public Node ContentHandler
        {
            get { return _contentHandler; }
        }
        public SenseNet.ContentRepository.Storage.Security.SecurityHandler Security
        {
            get { return _contentHandler.Security; }
        }
        /// <summary>
        /// Gets the field <see cref="System.Collections.Generic.Dictionary(string, Field)">Dictionary</see> of the instance.
        /// </summary>
        public IDictionary<string, Field> Fields
        {
            get { return _fields; }
        }
        public IEnumerable<FieldSetting> FieldSettings
        {
            get
            {
                if (_fieldSettings == null)
                    _fieldSettings = Fields.Values.Select(f => f.FieldSetting).ToArray();
                return _fieldSettings;
            }
        }
        /// <summary>
        /// Gets the friendly name of the Content or If it is null or empty, the value comes from the ContentTypeDefinition.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var dn = this._fields.ContainsKey("DisplayName") ? this["DisplayName"] as string : _contentHandler.DisplayName;
                return String.IsNullOrEmpty(dn) ? Content.Create(_contentType).DisplayName : dn;
            }
            set
            {
                _contentHandler.DisplayName = value;
            }
        }
        /// <summary>
        /// Gets the Description of Content. This value comes from the ContentTypeDefinition.
        /// </summary>
        public string Description
        {
            get
            {
                var desc = this._fields.ContainsKey("Description") ? this["Description"] as string : _contentHandler.GetPropertySafely("Description") as string;
                return String.IsNullOrEmpty(desc) ? Content.Create(_contentType).Description : desc;
            }
        }
        public string Icon
        {
            get
            {
                var genericHandler = ContentHandler as GenericContent;
                if (genericHandler != null)
                    return genericHandler.Icon;

                var ctypeHandler = ContentHandler as ContentType;
                if (ctypeHandler != null)
                    return ctypeHandler.Icon;

                return _contentType.Icon;
            }
        }
        /// <summary>
        /// Indicates the validity of the content. It is <c>true</c> if all contained fields are valid; otherwise, <c>false</c>.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!_isValidated)
                    Validate();
                return _isValid;
            }
        }

        public bool IsNew
        {
            get
            {
                var isdt = this.ContentHandler as ISupportsDynamicFields;

                // IsNew on Node: Id == 0
                // It is overridden in RuntimeContentHandler
                return isdt != null ? isdt.IsNewContent : this.ContentHandler.IsNew;
            }
        }

        public bool IsFolder
        {
            get { return this.ContentHandler is IFolder; }
        }

        public ChildrenDefinition ChildrenDefinition
        {
            get
            {
                var ct = ContentHandler as ContentType;
                if (ct != null)
                    return ct.ChildrenDefinition;

                var gc = ContentHandler as GenericContent;
                if (gc != null)
                    return gc.ChildrenDefinition;

                throw new SnNotSupportedException("Only GenericContent and ContentType support ChildrenDefinition.");
            }
            set
            {
                var ct = ContentHandler as ContentType;
                if (ct != null)
                {
                    ct.ChildrenDefinition = value;
                    return;
                }

                var gc = ContentHandler as GenericContent;
                if (gc != null)
                {
                    gc.ChildrenDefinition = value;
                    return;
                }
                throw new SnNotSupportedException("Only GenericContent and ContentType support ChildrenDefinition.");
            }
        }
        public virtual ISnQueryable<Content> Children
        {
            get
            {
                var ct = ContentHandler as ContentType;
                if (ct != null)
                    return ct.GetQueryableChildren();

                var gc = ContentHandler as GenericContent;
                if (gc != null)
                    return gc.GetQueryableChildren();

                throw new NotSupportedException("Only GenericContent and ContentType support queryable Children.");
            }
        }

        public IEnumerable<Node> Versions
        {
            get { return ContentHandler.LoadVersions(); }
        }

        public PropertyDescriptorCollection PropertyDescriptors { get; set; }

        // ------------------------------------------------------------------------- Shortcuts

        /// <summary>
        /// Gets the Id of the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// </summary>
        public int Id
        {
            get { return _contentHandler.Id; }
        }
        /// <summary>
        /// Gets the path of the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// </summary>
        public string Path
        {
            get { return _contentHandler.Path; }
        }
        /// <summary>
        /// Gets the name of the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// </summary>
        public string Name
        {
            get { return _contentHandler.Name; }
        }
        public int Index
        {
            get { return this.ContentHandler.Index; }
            set { this.ContentHandler.Index = value; }
        }
        public DateTime CreationDate
        {
            get { return this.ContentHandler.CreationDate; }
            set { this.ContentHandler.CreationDate = value; }
        }
        public DateTime ModificationDate
        {
            get { return this.ContentHandler.ModificationDate; }
            set { this.ContentHandler.ModificationDate = value; }
        }

        public bool IsContentList
        {
            get { return this.ContentHandler.ContentListType != null && this.ContentHandler.ContentListId == 0; }
        }
        public bool IsContentListItem
        {
            get { return this.ContentHandler.ContentListId != 0; }
        }
        public bool IsLastPublicVersion { get { return ContentHandler.IsLastPublicVersion; } }
        public bool IsLatestVersion { get { return ContentHandler.IsLatestVersion; } }

        /// <summary>
        /// Gets or sets the value of an indexed <see cref="SenseNet.ContentRepository.Field">Field</see>. 
        /// Type of return value is determined by derived <see cref="SenseNet.ContentRepository.Field">Field</see>.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns>An <see cref="System.Object">object</see> that represents the <see cref="SenseNet.ContentRepository.Field">Field</see>'s value.</returns>
        public object this[string fieldName]
        {
            get
            {
                if (!_fields.ContainsKey(fieldName))
                {
                    // Compensation logic: try to reload content fields to make sure that the field really does not exist.
                    // This workaround was created mainly for reloading Settings (or other dynamic) content that were 
                    // initialized improperly.
                    var extendedHandler = this.ContentHandler as ISupportsDynamicFields;
                    if (extendedHandler != null)
                    {
                        // memorize current (possibly incomplete) list of fields
                        var dfmd = extendedHandler.GetDynamicFieldMetadata();
                        var dynamicFieldsBeforeReload = dfmd == null ? new string[0] : dfmd.Keys.ToArray();

                        // clean contenthandler cache
                        extendedHandler.ResetDynamicFields();

                        // re-generate extended content type
                        var contentType = ContentTypeManager.Current.GetContentTypeByHandler(this.ContentHandler);
                        if (contentType != null)
                        {
                            contentType = ExtendContentType(extendedHandler, contentType);

                            // reset content type and fields
                            this.InitializeInstance(this.ContentHandler, contentType);

                            SnLog.WriteWarning(
                                $"Field reset executed on content {this.Path} because field {fieldName} was not found on the content.",
                                EventId.RepositoryRuntime,
                                properties: new Dictionary<string, object> 
                                { 
                                    { "DynamicFieldsBefore", string.Join(", ", dynamicFieldsBeforeReload) },
                                    { "DynamicFieldsAfter", string.Join(", ", extendedHandler.GetDynamicFieldMetadata().Keys) }
                                });
                        }
                    }

                    if (!_fields.ContainsKey(fieldName))
                        throw new InvalidOperationException(string.Format("Field not found. Content {0} of type {1}, field name: {2}", this.Path, this.ContentType.Path, fieldName));
                }

                return _fields[fieldName].GetData();
            }
            set
            {
                if (!_fields.ContainsKey(fieldName))
                    throw new Exception("Field not found. Content of type " + this.ContentType.Name + ", field name: " + fieldName);

                _fields[fieldName].SetData(value);
            }
        }
        public string GetStoredValue(string fieldName)
        {
            Field field;
            if (!_fields.TryGetValue(fieldName, out field))
                return string.Empty;
            return field.GetStoredValue();
        }
        public string GetLocalizedValue(string fieldName, CultureInfo cultureInfo = null)
        {
            Field field;
            if (!_fields.TryGetValue(fieldName, out field))
                return string.Empty;
            return field.GetLocalizedValue(cultureInfo);
        }

        public string WorkspaceName
        {
            get
            {
                var gc = this.ContentHandler as GenericContent;
                return gc == null ? string.Empty : gc.WorkspaceName;
            }
        }

        public string WorkspaceTitle
        {
            get
            {
                var gc = this.ContentHandler as GenericContent;
                return gc == null ? string.Empty : gc.WorkspaceTitle;
            }
        }

        public string WorkspacePath
        {
            get
            {
                var gc = this.ContentHandler as GenericContent;
                return gc == null ? string.Empty : gc.WorkspacePath;
            }
        }

        public CheckInCommentsMode CheckInCommentsMode
        {
            get
            {
                var gc = this.ContentHandler as GenericContent;
                return gc == null ? CheckInCommentsMode.None : gc.CheckInCommentsMode;
            }
        }

        public Dictionary<string, Field> AspectFields { get; private set; }

        public bool IsAllowedField(string fieldName)
        {
            // check aspect fields
            if (AspectFields != null && AspectFields.ContainsKey(fieldName))
                return !ContentHandler.IsHeadOnly;

            // check fields without underlying properties (e.g. Icon)
            if (!this.ContentHandler.HasProperty(fieldName) && this.Fields.ContainsKey(fieldName))
            {
                // Size is a field but not a property. It must be inaccessible even with Preview permissions,
                // because it relies on the Binary field that is forbidden for preview-only users.
                if (fieldName == "Size")
                    return !ContentHandler.IsHeadOnly && !ContentHandler.IsPreviewOnly;

                return !ContentHandler.IsHeadOnly;
            }

            // check properties
            return ContentHandler.IsAllowedProperty(fieldName);
        }

        internal bool ImportingExplicitVersion { get; set; }

        // ========================================================================= Construction

        private Content(Node contentHandler, ContentType contentType)
        {
            InitializeInstance(contentHandler, contentType);
        }

        protected virtual void InitializeInstance(Node contentHandler, ContentType contentType)
        {
            _contentHandler = contentHandler;
            _contentType = contentType;
            _fields = new Dictionary<string, Field>();

            Content targetContent = null;
            var cLink = contentHandler as ContentLink;
            if (cLink != null)
                targetContent = cLink.LinkedContent == null ? null : Content.Create(cLink.LinkedContent);

            if (targetContent != null)
                InitializeFieldsWithContentLink(contentType, targetContent, cLink);
            else
                InitializeFields(contentType);

            if (_contentType == null)
                throw new ArgumentNullException("contentType");
            if (_contentType.Name == null)
                throw new InvalidOperationException("ContentType name is null");

            // field collection of the temporary fieldsetting content
            // or journal node must not contain the ContentList fields
            if (contentHandler is FieldSettingContent || _contentType.Name.CompareTo("JournalNode") == 0)
                return;

            ContentList list = null;

            try
            {
                if (contentHandler.ContentListId != 0)
                {
                    using (new SystemAccount())
                    {
                        list = contentHandler.LoadContentList() as ContentList;
                    }
                }

                if (list == null)
                    return;
            }
            catch (Exception ex)
            {
                // handle errors that occur during heavy load
                if (contentHandler == null)
                    throw new ArgumentNullException("Content handler became null.", ex);

                throw new InvalidOperationException("Error during content list load.", ex);
            }

            try
            {
                foreach (var fieldSetting in list.FieldSettings)
                {
                    var field = Field.Create(this, fieldSetting);
                    _fields.Add(field.Name, field);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error during content list field creation.", ex);
            }

        }
        private void InitializeFields(ContentType contentType)
        {
            foreach (var fieldSetting in contentType.FieldSettings)
            {
                var field = Field.Create(this, fieldSetting);
                _fields.Add(field.Name, field);
            }
            BuildAspectFields();
        }

        private void InitializeFieldsWithContentLink(ContentType contentType, Content linkedContent, ContentLink contentLink)
        {
            var linkedType = linkedContent.ContentType;
            var notLinkedFields = contentLink.NotLinkedFields;
            foreach (var fieldSetting in linkedType.FieldSettings)
            {
                if (notLinkedFields.Contains(fieldSetting.Name))
                    continue;
                var field = Field.Create(linkedContent, fieldSetting);
                field.IsLinked = true;
                _fields.Add(field.Name, field);
            }
            foreach (var fieldSetting in contentType.FieldSettings)
            {
                if (_fields.ContainsKey(fieldSetting.Name))
                    continue;
                var field = Field.Create(this, fieldSetting);
                _fields.Add(field.Name, field);
            }
            BuildAspectFields();
        }



        /// <summary>
        /// Loads the appropiate <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> by the given ID and wraps to a <c>Content</c>.
        /// </summary>
        /// <returns>The latest version of the <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> that has the given ID wrapped by a <c>Content</c> instance.</returns>
        public static Content Load(int id)
        {
            Node node = Node.LoadNode(id);
            if (node == null)
                return null;
            return Create(node);
        }
        /// <summary>
        /// Loads the appropiate <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> by the given Path and wraps to a <c>Content</c>.
        /// </summary>
        /// <returns>The latest version of the <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> that has the given Path wrapped by a <c>Content</c> instance.</returns>
        public static Content Load(string path)
        {
            Node node = Node.LoadNode(path);
            if (node == null)
                return null;
            return Create(node);
        }
        /// <summary>
        /// Loads the appropiate <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> by the given ID and version number and wraps to a <c>Content</c>.
        /// </summary>
        /// <returns>The given version of the <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> that has the given ID wrapped by a <c>Content</c>.</returns>
        public static Content Load(int id, VersionNumber version)
        {
            Node node = Node.LoadNode(id, version);
            if (node == null)
                return null;
            return Create(node);
        }
        /// <summary>
        /// Loads the appropiate <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> by the given Path and version number and wraps to a <c>Content</c>.
        /// </summary>
        /// <returns>The given version of the <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> that has the given Path wrapped by a <c>Content</c>.</returns>
        public static Content Load(string path, VersionNumber version)
        {
            Node node = Node.LoadNode(path, version);
            if (node == null)
                return null;
            return Create(node);
        }

        public static Content LoadByIdOrPath(string idOrPath)
        {
            var node = Node.LoadNodeByIdOrPath(idOrPath);
            return node != null ? Content.Create(node) : null;
        }

        [Obsolete("Use Content.Create instead")]
        public Content()
        {

        }
        /// <summary>
        /// Creates a <c>Content</c> instance from an instantiated <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// </summary>
        /// <returns>Passed <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> wrapped by a <c>Content</c>.</returns>
        public static Content Create(Node contentHandler)
        {
            return Create<Content>(contentHandler);
        }

        public static Content Create<T>(Node contentHandler) where T : Content, new()
        {
            if (contentHandler == null)
                throw new ArgumentNullException("contentHandler");
            ContentType contentType = ContentTypeManager.Current.GetContentTypeByHandler(contentHandler);
            if (contentType == null)
            {
                var rtch = contentHandler as RuntimeContentHandler;
                if (rtch != null)
                    return new Content(rtch, rtch.ContentType);
                if (contentHandler.Name == typeof(ContentType).Name)
                    contentType = contentHandler as ContentType;
                if (contentType == null)
                    throw new ApplicationException(String.Concat(SR.Exceptions.Content.Msg_UnknownContentType, ": ", contentHandler.NodeType.Name));
            }
            var extendedHandler = contentHandler as ISupportsDynamicFields;
            if (extendedHandler != null)
                contentType = ExtendContentType(extendedHandler, contentType);
            var result = new T();
            result.InitializeInstance(contentHandler, contentType);
            return result;
        }

        private static ContentType ExtendContentType(ISupportsDynamicFields extendedHandler, ContentType contentType)
        {
            var extendedContentType = ContentType.Create(extendedHandler, contentType);
            if (extendedContentType == null)
                throw new ApplicationException("Cannot create content from a " + extendedHandler.GetType().FullName);
            return extendedContentType;
        }
        /// <summary>
        /// Creates an appropriate new <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> instance by the given parameters ant wraps to a <c>Content</c>.
        /// This method calls the appropriate constructor determined by the passed arguments but do not saves the new <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// </summary>
        /// <param name="contentTypeName">Determines the type of <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>. 
        /// Fully qualified type name is contained by the <see cref="SenseNet.ContentRepository.Schema.ContentType">ContentType</see> named this parameter's value.
        /// </param>
        /// <param name="parent"><see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> instance as a parent in the big tree.</param>
        /// <param name="name">The expected name of the new <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.</param>
        /// <param name="args">Additional parameters required by the expected constructor of <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// In this version neither argument can be null.</param>
        /// <returns>An instantiated <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> wrapped by a <c>Content</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any custom argument is null.</exception>
        public static Content CreateNew(string contentTypeName, Node parent, string nameBase, params object[] args)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Content.CreateNew"))
            {
                if (args == null)
                    args = new object[0];

                ContentType contentType = ContentTypeManager.Current.GetContentTypeByName(contentTypeName);
                if (contentType == null)
                    throw new ApplicationException(String.Concat(SR.Exceptions.Content.Msg_UnknownContentType, ": ", contentTypeName));
                Type type = TypeResolver.GetType(contentType.HandlerName);

                Type[] signature = new Type[args.Length + 2];
                signature[0] = typeof(Node);
                signature[1] = typeof(string);
                object[] arguments = new object[signature.Length];
                arguments[0] = parent;
                arguments[1] = contentTypeName;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == null)
                        throw new ArgumentOutOfRangeException("args", SR.Exceptions.Content.Msg_CannotCreateNewContentWithNullArgument);
                    signature[i + 2] = args[i].GetType();
                    arguments[i + 2] = args[i];
                }

                var ctorInfo = type.GetConstructor(signature);
                Node node = null;
                var nodeCreateRetryCount = 0;
                Exception nodeCreateException = null;

                while (true)
                {
                    try
                    {
                        node = (Node)ctorInfo.Invoke(arguments);

                        // log previous exception if exists
                        if (nodeCreateException != null)
                            SnLog.WriteWarning("Error during node creation: " + RepositoryTools.CollectExceptionMessages(nodeCreateException));

                        break;
                    }
                    catch (Exception ex)
                    {
                        // store the exception for later use
                        nodeCreateException = ex;

                        // retry a few times to handle errors that occur during heavy load
                        nodeCreateRetryCount++;

                        if (nodeCreateRetryCount > 2)
                            throw new Exception(string.Format("Node creation failed. ContentType name: {0}, Parent: {1}, Name: {2}",
                                contentTypeName ?? string.Empty, parent == null ? "NULL" : parent.Path, nameBase ?? string.Empty), ex);

                        Thread.Sleep(10);
                    }
                }

                var name = ContentNamingProvider.GetNewName(nameBase, contentType, parent);

                if (!string.IsNullOrEmpty(name))
                    node.Name = name;

                op.Successful = true;

                // try to re-use the already created content in GenericContent
                var gc = node as GenericContent;

                return gc == null ? Content.Create(node) : gc.Content;
            }
        }

        public static Content CreateNewAndParse(string contentTypeName, Node parent, string name, Dictionary<string, string> fieldData)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Content.CreateNewAndParse"))
            {
                var content = CreateNew(contentTypeName, parent, name);
                Modify(content, fieldData);

                op.Successful = true;
                return content;
            }
        }


        // ========================================================================= Methods
        public static void Modify(Content content, Dictionary<string, string> fieldData)
        {
            using (var op = SnTrace.ContentOperation.StartOperation("Content.Modify"))
            {
                var ok = true;
                foreach (var fieldName in fieldData.Keys)
                {
                    if (!content.Fields.ContainsKey(fieldName))
                        throw new ApplicationException("Unknown field: " + fieldName);
                    ok &= content.Fields[fieldName].Parse(fieldData[fieldName]);
                }
                if (!ok)
                {
                    content._isValidated = true;
                    content._isValid = false;
                }
                else
                {
                    content.Validate();
                }
                op.Successful = true;
            }
        }

        /// <summary>
        /// Returns an array contains all existing <see cref="SenseNet.ContentRepository.Schema.ContentType">ContentType</see> name.
        /// </summary>
        public static string[] GetContentTypeNames()
        {
            Dictionary<string, ContentType> contentTypes = ContentTypeManager.Current.ContentTypes;
            string[] names = new string[contentTypes.Count];
            contentTypes.Keys.CopyTo(names, 0);
            return names;
        }

        public Content GetContentList()
        {
            if (!this.IsContentList)
                return null;
            var listNode = this.ContentHandler.LoadContentList();
            return Content.Create(listNode);
        }

        /// <summary>
        /// Valiadates each contained <see cref="SenseNet.ContentRepository.Field">Field</see>s and returns <c>true</c> if all fields are valid.
        /// </summary>
        /// <returns>It is <c>true</c> if all contained <see cref="SenseNet.ContentRepository.Field">Field</see>s are valid; otherwise, <c>false</c>.</returns>
        public bool Validate()
        {
            if (_isValidated)
                return _isValid;

            _isValid = true;
            foreach (var item in _fields)
                _isValid = _isValid & item.Value.Validate();

            _isValidated = true;

            return _isValid;
        }
        internal void FieldChanged()
        {
            _isValidated = false;
        }
        private void SaveFields()
        {
            SaveFields(true);
        }
        private void SaveFields(bool validOnly)
        {
            XmlWriter aspectWriter = null;
            StringWriter stringWriter = null;
            if (AspectFields != null && AspectFields.Count > 0)
            {
                stringWriter = new StringWriter();
                aspectWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true });
                aspectWriter.WriteStartDocument();
                aspectWriter.WriteStartElement("AspectData");

                foreach (var field in this.AspectFields.Values)
                    if (!field.FieldSetting.Bindings.Any() || field.FieldSetting.Bindings[0] == field.FieldSetting.Name)
                        field.Export2(aspectWriter, null);

                aspectWriter.WriteEndElement();
                aspectWriter.WriteEndDocument();
                aspectWriter.Flush();
                var aspectData = stringWriter.GetStringBuilder().ToString();

                var gc = (GenericContent)this.ContentHandler;
                gc.AspectData = aspectData;
            }

            _isValid = true;
            foreach (string key in _fields.Keys)
            {
                Field field = _fields[key];
                field.Save(validOnly);
                _isValid = _isValid && field.IsValid;
            }
            _isValidated = true;
        }

        /// <summary>
        /// Validates and saves the wrapped <c>ContentHandler</c> into the Sense/Net Content Repository with considering the versioning settings.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If <c>Content</c> is not valid 
        ///         throws an <see cref="InvalidContentException">InvalidContentException</see>.
        ///     </item>
        ///     <item>
        ///         Saves the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> into the Sense/Net Content Repository.
        ///     </item>
        /// </list>
        /// 
        /// If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> after the saving
        /// its version is depends its <see cref="SenseNet.ContentRepository.GenericContent.VersioningMode">VersioningMode</see> setting.
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <c>Content</c> is invalid.</exception>
        public void Save()
        {
            Save(true);
        }

        public void Save(bool validOnly)
        {
            if (_contentHandler.Locked)
                Save(validOnly, SavingMode.KeepVersion);
            else
                Save(validOnly, SavingMode.RaiseVersion);
        }

        public void Save(SavingMode mode)
        {
            Save(true, mode);
        }

        /// <summary>
        /// Saves the wrapped <c>ContentHandler</c> into the Sense/Net Content Repository with considering the versioning settings.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If passed <paramref name="validOnly">validOnly</paramref> parameter is true  and <c>Content</c> is not valid 
        ///         throws an <see cref="InvalidContentException">InvalidContentException</see>
        ///     </item>
        ///     <item>
        ///         Saves the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> into the Sense/Net Content Repository.
        ///     </item>
        /// </list>
        /// 
        /// If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> after the saving
        /// its version is depends its <see cref="SenseNet.ContentRepository.GenericContent.VersioningMode">VersioningMode</see> setting.
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <paramref name="validOnly"> is true  and<c>Content</c> is invalid.</exception>
        public void Save(bool validOnly, SavingMode mode)
        {
            SaveFields(validOnly);
            if (validOnly && !IsValid)
                throw InvalidContentExceptionHelper();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent != null)
                genericContent.Save(mode);
            else
                _contentHandler.Save();

            foreach (string key in _fields.Keys)
                _fields[key].OnSaveCompleted();

            var template = _contentHandler.Template;
            if (template != null)
            {
                ContentTemplate.CopyContents(this);
            }
        }

        /// <summary>
        /// Ends the multistep saving process and makes the content available for modification.
        /// </summary>
        public void FinalizeContent()
        {
            this.ContentHandler.FinalizeContent();
        }

        /// <summary>
        /// Validates and saves the wrapped <c>ContentHandler</c> into the Sense/Net Content Repository without considering the versioning settings.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If <c>Content</c> is not valid throws an <see cref="InvalidContentException">InvalidContentException</see>.
        ///     </item>
        ///     <item>
        ///         Saves the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> into the Sense/Net Content Repository.
        ///     </item>
        /// </list>
        /// 
        /// After the saving the version of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> will not changed.
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <c>Content</c> is invalid.</exception>
        public void SaveSameVersion()
        {
            SaveSameVersion(true);
        }
        /// <summary>
        /// Validates and saves the wrapped <c>ContentHandler</c> into the Sense/Net Content Repository without considering the versioning settings.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If passed <paramref name="validOnly">validOnly</paramref> parameter is true  and <c>Content</c> is not valid 
        ///         throws an <see cref="InvalidContentException">InvalidContentException</see>
        ///     </item>
        ///     <item>
        ///         Saves the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> into the Sense/Net Content Repository.
        ///     </item>
        /// </list>
        /// 
        /// After the saving the version of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> will not changed.
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <paramref name="validOnly"> is true  and<c>Content</c> is invalid.</exception>
        public void SaveSameVersion(bool validOnly)
        {
            SaveFields(validOnly);
            if (validOnly && !IsValid)
                throw InvalidContentExceptionHelper();
            GenericContent genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Save();
            else
                genericContent.Save(SavingMode.KeepVersion);

            var template = _contentHandler.Template;
            if (template != null)
            {
                ContentTemplate.CopyContents(this);
            }
        }

        public void SaveExplicitVersion(bool validOnly = true)
        {
            SaveFields(validOnly);
            if (validOnly && !IsValid)
                throw InvalidContentExceptionHelper();
            GenericContent genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                throw new InvalidOperationException("Only a generic content can be saved with explicit version.");
            else
                genericContent.SaveExplicitVersion();

            var template = _contentHandler.Template;
            if (template != null)
                ContentTemplate.CopyContents(this);
        }

        /// <summary>
        /// Validates and publishes the wrapped <c>ContentHandler</c> if it is a <c>GenericContent</c> otherwise saves it normally.
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If <c>Content</c> is not valid throws an <see cref="InvalidContentException">InvalidContentException</see>.
        ///     </item>
        ///     <item>
        ///         If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        ///         the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> calls its
        ///         <see cref="SenseNet.ContentRepository.GenericContent.Publish">Publish</see> method otherwise saves it normally.
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <c>Content</c> is invalid.</exception>
        public void Publish()
        {
            SaveFields();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Save();
            else
                genericContent.Publish();
        }
        public void Approve()
        {
            SaveFields();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Save();
            else
                genericContent.Approve();
        }
        public void Reject()
        {
            SaveFields();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Save();
            else
                genericContent.Reject();
        }
        /// <summary>
        /// Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        /// 
        /// If <c>Content</c> is not valid throws an <see cref="InvalidContentException">InvalidContentException</see>.
        /// 
        /// If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> calls its
        /// <see cref="SenseNet.ContentRepository.GenericContent.CheckIn">CheckIn</see> method otherwise calls the
        /// <see cref="SenseNet.ContentRepository.Storage.Node.Lock.Unlock">Unlock</see> method with
        /// <c><see cref="SenseNet.ContentRepository.Storage.VersionStatus">VersionStatus</see>.Public</c> and 
        /// <c><see cref="SenseNet.ContentRepository.Storage.VersionRaising">VersionRaising</see>.None</c> parameters.
        /// 
        /// </summary>
        /// <remarks>
        /// This method executes followings:
        /// <list type="bullet">
        ///     <item>
        ///         Saves all <see cref="SenseNet.ContentRepository.Field">Field</see>s into the properties 
        ///         of wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>.
        ///     </item>
        ///     <item>
        ///         If <c>Content</c> is not valid throws an <see cref="InvalidContentException">InvalidContentException</see>.
        ///     </item>
        ///     <item>
        /// 		If the wrapped <see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see> inherited from 
        /// 		the <see cref="SenseNet.ContentRepository.GenericContent">GenericContent</see> calls its
        /// 		<see cref="SenseNet.ContentRepository.GenericContent.CheckIn">CheckIn</see> method otherwise calls the
        /// 		<see cref="SenseNet.ContentRepository.Storage.Node">ContentHandler</see>'s
        /// 		<see cref="SenseNet.ContentRepository.Storage.Security.LockHandler.Unlock(VersionStatus, VersionRaising)">Lock.Unlock</see> method with
        /// 		<c><see cref="SenseNet.ContentRepository.Storage.VersionStatus">VersionStatus</see>.Public</c> and 
        /// 		<c><see cref="SenseNet.ContentRepository.Storage.VersionRaising">VersionRaising</see>.None</c> parameters.
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidContentException">Thrown when <c>Content</c> is invalid.</exception>
        public void CheckIn()
        {
            SaveFields();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Lock.Unlock(VersionStatus.Approved, VersionRaising.None);
            else
                genericContent.CheckIn();
        }

        public void CheckOut()
        {
            SaveFields();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Lock.Lock();
            else
                genericContent.CheckOut();
        }

        public void UndoCheckOut()
        {
            SaveFields();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Lock.Unlock(VersionStatus.Approved, VersionRaising.None);
            else
                genericContent.UndoCheckOut();
        }

        public void ForceUndoCheckOut()
        {
            if (!SavingAction.HasForceUndoCheckOutRight(this.ContentHandler))
                throw new Storage.Security.SenseNetSecurityException(this.Path, Storage.Security.PermissionType.ForceCheckin);

            SaveFields();

            var genericContent = _contentHandler as GenericContent;
            if (genericContent == null)
                _contentHandler.Lock.Unlock(VersionStatus.Approved, VersionRaising.None);
            else
                genericContent.UndoCheckOut();
        }

        public void DontSave()
        {
            // :)
        }

        public bool Approvable
        {
            get
            {
                var genericContent = _contentHandler as GenericContent;

                return genericContent != null && genericContent.Approvable;
            }
        }

        public bool Publishable
        {
            get
            {
                var genericContent = _contentHandler as GenericContent;

                return genericContent != null && genericContent.Publishable;
            }
        }

        /// <summary>
        /// Adds fields on the fly if that is supported by the current content item.
        /// </summary>
        /// <param name="fields">The fields to add.</param>
        /// <returns>Whether the operation was successful.</returns>
        public bool AddFieldsOnTheFly(IEnumerable<FieldMetadata> fields)
        {
            if (!this.SupportsAddingFieldsOnTheFly)
                return false;
            if (!((ISupportsAddingFieldsOnTheFly)this.ContentHandler).AddFields(fields))
                return false;

            // Save field values (without validation for now; the final Save call will validate anyway)
            SaveFields(false);

            // Find the content type of this instance
            ContentType contentType = ContentTypeManager.Current.GetContentTypeByHandler(this.ContentHandler);
            if (contentType == null)
            {
                var rtch = this.ContentHandler as RuntimeContentHandler;
                if (rtch != null)
                    contentType = rtch.ContentType;
                if (this.ContentHandler.Name == typeof(ContentType).Name)
                    contentType = this.ContentHandler as ContentType;
                if (contentType == null)
                    throw new ApplicationException(String.Concat(SR.Exceptions.Content.Msg_UnknownContentType, ": ", this.ContentHandler.NodeType.Name));
            }

            var extendedHandler = this.ContentHandler as ISupportsDynamicFields;
            if (extendedHandler != null)
                contentType = ExtendContentType(extendedHandler, contentType);

            // Re-initialize this Content instance
            this.InitializeInstance(this.ContentHandler, contentType);

            return true;
        }

        /// <summary>
        /// Deletes the Node and all of its contents from the database. This operation removes all child nodes too.
        /// </summary>
        /// <param name="contentId">Identifier of the Node that will be deleted.</param>
        public static void DeletePhysical(int contentId)
        {
            Node.ForceDelete(contentId);
        }
        /// <summary>
        /// Deletes the Node and all of its contents from the database. This operation removes all child nodes too.
        /// </summary>
        /// <param name="path">The path of the Node that will be deleted.</param>
        public static void DeletePhysical(string path)
        {
            Node.ForceDelete(path);
        }
        /// <summary>
        /// Deletes the represented <see cref="SenseNet.ContentRepository.Storage.Node">Node</see> and all of its contents from the database. This operation removes all child nodes too.
        /// </summary>
        public void DeletePhysical()
        {
            ForceDelete();
        }

        public static void Delete(int contentId)
        {
            Node.Delete(contentId);
        }

        public static void Delete(string path)
        {
            Node.Delete(path);
        }

        public void Delete()
        {
            this.ContentHandler.Delete();
        }

        public void ForceDelete()
        {
            this.ContentHandler.ForceDelete();
        }

        public void Delete(bool byPassTrash)
        {
            if (!byPassTrash)
            {
                this.ContentHandler.Delete();
            }
            else
            {
                // only GenericContent has a byPassTrash functinality
                var gc = this.ContentHandler as GenericContent;
                if (gc != null)
                    gc.Delete(byPassTrash);
                else
                    this.ContentHandler.Delete();
            }
        }

        private Exception InvalidContentExceptionHelper()
        {
            var fields = new Field[Fields.Count];
            Fields.Values.CopyTo(fields, 0);

            return new InvalidContentException(String.Concat("Cannot save the Content. Invalid Fields: ",
                String.Join(", ", (from field in fields where !field.IsValid select field.DisplayName ?? field.Name).ToArray())));
        }

        [Obsolete("Use the methods of the ContentNamingProvider class instead")]
        public static string GenerateNameFromTitle(string parent, string title)
        {
            return ContentNamingProvider.GetNameFromDisplayName(title);
        }

        [Obsolete("Use the methods of the ContentNamingProvider class instead")]
        public static string GenerateNameFromTitle(string title)
        {
            return ContentNamingProvider.GetNameFromDisplayName(title);
        }

        /// <summary>
        /// Rebuilds the Lucene index document of a content and optionally of all documents in the whole subtree. 
        /// In case the value of <value>rebuildLevel</value> is <value>IndexOnly</value> the index document is refreshed 
        /// based on the already existing extracted data stored in the database. This is a significantly faster method 
        /// and it is designed for cases when only the place of the content in the tree has changed or the index got corrupted.
        /// The <value>DatabaseAndIndex</value> algorithm will reindex the full content than update the Lucene index in the
        /// file system the same way as the light-weight algorithm.
        /// </summary>
        /// <param name="recursive">Whether child content should be reindexed or not. Default: false.</param>
        /// <param name="rebuildLevel">The algorithm selector. Value can be <value>IndexOnly</value> or <value>DatabaseAndIndex</value>. Default: <value>IndexOnly</value></param>
        public void RebuildIndex(bool recursive = false, IndexRebuildLevel rebuildLevel = IndexRebuildLevel.IndexOnly)
        {
            StorageContext.Search.SearchEngine.GetPopulator().RebuildIndex(this.ContentHandler, recursive, rebuildLevel);
        }

        /*-------------------------------------------------------------------------- SnLinq */

        public static ContentSet<Content> All { get { return new ContentSet<Content>(new ChildrenDefinition { PathUsage = PathUsageMode.NotUsed }, null); } }

        public bool InFolder(string path) { return ContentHandler.InFolder(path); }
        public bool InFolder(Node node) { return InFolder(node.Path); }
        public bool InFolder(Content content) { return InFolder(content.Path); }
        public bool InTree(string path) { return ContentHandler.InTree(path); }
        public bool InTree(Node node) { return InTree(node.Path); }
        public bool InTree(Content content) { return InTree(content.Path); }
        public bool Type(string contentTypeName) { return ContentType.Name == contentTypeName; }
        public bool TypeIs(string contentTypeName) { return ContentType.IsInstaceOfOrDerivedFrom(contentTypeName); }

        /*-------------------------------------------------------------------------- Transfer Methods */

        // ---- for powershell provider
        public static Content Import(
            string data,
            int contentId,
            string parentPath,
            string name,
            string contentTypeName,
            bool withReferences,
            bool onlyReferences,
            out string[] referenceFields)
        {
            var references = new List<string>();
            var xml = new XmlDocument();
            xml.LoadXml(data);

            XmlNode nameNode = xml.SelectSingleNode("/ContentMetaData/ContentName");

            var clearPermissions = xml.SelectSingleNode("/ContentMetaData/Permissions/Clear") != null;
            var hasBreakPermissions = xml.SelectSingleNode("/ContentMetaData/Permissions/Break") != null;
            var hasPermissions = xml.SelectNodes("/ContentMetaData/Permissions/Identity").Count > 0;

            Content content = null;
            if (contentId > 0)
            {
                content = Content.Load(contentId);
                if (content == null)
                    throw new ApplicationException("Content does not exist. Id: " + contentId);
            }
            else
            {
                var path = RepositoryPath.Combine(parentPath, name);
                content = Content.Load(path);
                if (content == null)
                {
                    var parent = Node.LoadNode(parentPath);
                    if (parent == null)
                        throw new ContentNotFoundException(parentPath);
                    content = Content.CreateNew(contentTypeName, parent, name);
                }
            }
            var changed = content.Id == 0;
            var nodeList = xml.SelectNodes("/ContentMetaData/Fields/*");
            foreach (XmlNode fieldNode in nodeList)
            {
                var subType = FieldSubType.General;
                var subTypeString = ((XmlElement)fieldNode).GetAttribute("subType");
                if (subTypeString.Length > 0)
                    subType = (FieldSubType)Enum.Parse(typeof(FieldSubType), subTypeString);
                var fieldName = Field.ParseImportName(fieldNode.LocalName, subType);

                Field field;
                if (!content.Fields.TryGetValue(fieldName, out field))
                    throw new TransferException(true, "Field not found", content.ContentHandler.Path, content.ContentHandler.NodeType.Name, fieldName);

                var isReference = field is ReferenceField;
                if (isReference)
                    references.Add(field.Name);
                if (isReference && !withReferences)
                    continue;
                if (!isReference && onlyReferences)
                    continue;

                try
                {
                    field.Import(fieldNode);
                    changed = true;
                }
                catch (ReferenceNotFoundException ex)
                {
                    // skip missing user reference according to config
                    if (RepositoryEnvironment.SkipImportingMissingReferences && RepositoryEnvironment.SkipReferenceNames.Contains(field.Name))
                    {
                        SnLog.WriteException(ex);

                        // log this to the screen or log file if exists
                        RepositoryInstance.Instance?.Console?.WriteLine("---------- Reference skipped: " + field.Name);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            referenceFields = references.ToArray();
            return content;
        }

        // for old-way-importer
        public bool ImportFieldData(ImportContext context)
        {
            return ImportFieldData(context, true);
        }
        public bool ImportFieldData(ImportContext context, bool saveContent)
        {
            bool changed = context.IsNewContent;

            // first of all: exporting aspect names if needed
            if (context.UpdateReferences)
            {
                foreach (XmlNode fieldNode in context.FieldData)
                {
                    if (fieldNode.LocalName == "Aspects")
                    {
                        var gc = ContentHandler as GenericContent;
                        if (gc != null)
                            ImportAspects(fieldNode, gc);
                        break;
                    }
                }
            }
            // exporting any other fields
            foreach (XmlNode fieldNode in context.FieldData)
            {
                var subType = FieldSubType.General;
                var subTypeString = ((XmlElement)fieldNode).GetAttribute("subType");
                if (subTypeString.Length > 0)
                    subType = (FieldSubType)Enum.Parse(typeof(FieldSubType), subTypeString);
                var fieldName = Field.ParseImportName(fieldNode.LocalName, subType);

                // if the content already exists, the field is an aspect field of an already existing aspect, DO NOT IMPORT IT
                if (!context.UpdateReferences && this.AspectFields != null && this.AspectFields.Any(f => f.Value.Name == fieldName))
                    continue;

                // This field has already imported or skipped
                if (fieldName == "Aspects")
                    continue;

                Field field;
                bool hasField = this.Fields.TryGetValue(fieldName, out field);

                // If the field is not present on the content item but it can add fields on the fly, try doing that
                if (!hasField && this.SupportsAddingFieldsOnTheFly)
                {
                    var fieldSetting = FieldSetting.InferFieldSettingFromXml(fieldNode, fieldName);
                    var fieldMetadata = new FieldMetadata(true, true, fieldName, fieldName, fieldSetting);
                    hasField = this.AddFieldsOnTheFly(new[] { fieldMetadata }) && this.Fields.TryGetValue(fieldName, out field);
                }

                // this is an aspect field that belongs to an aspect that is not imported yet --> skip it
                if (!hasField && fieldName.Contains((".")))
                    continue;

                if (!hasField)
                    throw new TransferException(true, "Field not found and could not be added.", this.ContentHandler.Path, this.ContentHandler.NodeType.Name, fieldName);

                var refField = field as ReferenceField;
                if (!context.UpdateReferences)
                {
                    field.Import(fieldNode, context);
                    changed = true;
                }
                else
                {
                    if (field.IsAspectField)
                    {
                        field.Import(fieldNode, context);
                        changed = true;
                    }
                    else if (refField != null)
                    {
                        try
                        {
                            field.Import(fieldNode, context);
                            changed = true;

                            if (field.Name == "CreatedBy" || field.Name == "ModifiedBy")
                            {
                                var fdata = field.GetData();
                                var refNodes = fdata as IEnumerable<Node>;
                                var refNode = refNodes == null ? fdata as Node : refNodes.FirstOrDefault();

                                if (refNode != null)
                                {
                                    if (field.Name == "CreatedBy")
                                        this.ContentHandler.CreatedBy = refNode;
                                    if (field.Name == "ModifiedBy")
                                        this.ContentHandler.ModifiedBy = refNode;
                                }
                            }
                        }
                        catch (ReferenceNotFoundException ex)
                        {
                            // skip missing user reference according to config
                            if (RepositoryEnvironment.SkipImportingMissingReferences && RepositoryEnvironment.SkipReferenceNames.Contains(refField.Name))
                            {
                                SnLog.WriteException(ex);

                                // log this to the screen or log file if exists
                                RepositoryInstance.Instance?.Console?.WriteLine("---------- Reference skipped: " + refField.Name);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
            }

            if (!changed)
                return true;

            SaveFields(context.NeedToValidate);
            if (context.NeedToValidate)
            {
                if (!this.IsValid)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string key in this.Fields.Keys)
                    {
                        Field field = this.Fields[key];
                        if (!field.IsValid)
                        {
                            sb.Append(field.GetValidationMessage());
                            sb.Append(Environment.NewLine);
                        }
                    }
                    context.ErrorMessage = sb.ToString();
                    return false;
                }
            }

            if (saveContent)
            {
                if (ImportingExplicitVersion)
                    this.SaveExplicitVersion();
                else if (context.IsNewContent)
                    this.SaveSameVersion();
                else
                    this.Save(context.NeedToValidate);
            }

            return true;
        }
        private void ImportAspects(XmlNode fieldNode, GenericContent gc)
        {
            var aspectNames = fieldNode.InnerText.Split(',').Select(x => x.Trim()).ToArray();
            var aspects = new Aspect[aspectNames.Length];
            for (int i = 0; i < aspectNames.Length; i++)
            {
                aspects[i] = Aspect.LoadAspectByName(aspectNames[i]);
                if (aspects[i] == null)
                    throw new TransferException(true, "Aspect not found: " + aspectNames[i], this.ContentHandler.Path, this.ContentHandler.NodeType.Name, "Aspects");
            }
            this.AddAspects(aspects);
        }
        public void ExportFieldData(XmlWriter writer, ExportContext context)
        {
            // first of all: exporting aspect names
            var gc = ContentHandler as GenericContent;
            if (gc != null)
            {
                var aspects = gc.Aspects.ToArray();
                if (aspects.Length > 0)
                    writer.WriteElementString("Aspects", String.Join(", ", aspects.Select(x => x.Name)));
            }

            // exporting other fields
            if (this.ContentHandler is ContentType)
                return;
            foreach (var field in this.Fields.Values)
                if (field.Name != "Name" && field.Name != "Versions")
                    field.Export(writer, context);
        }
        public void ExportFieldData2(XmlWriter writer, ExportContext context)
        {
            if (this.ContentHandler is ContentType)
                return;
            foreach (var field in this.Fields.Values)
                if (field.Name != "Name" && field.Name != "Versions")
                    field.Export2(writer, context);
        }

        // ------------------------------------------------------------------------- Xml Methods

        protected override void WriteXml(XmlWriter writer, bool withChildren, SerializationOptions options)
        {
            WriteXmlHeaderAndFields(writer, options);

            if (withChildren)
            {
                if (XmlWriterExtender != null)
                    XmlWriterExtender(writer);

                writer.WriteStartElement("Children");

                WriteXml(this.Children, writer, options);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public Action<XmlWriter> XmlWriterExtender;
        private const string FieldXmlCacheKey = "ContentFieldXml";

        private void WriteXmlHeaderAndFields(XmlWriter writer, SerializationOptions options)
        {
            writer.WriteStartElement("Content");
            base.WriteHead(writer, this.ContentType.Name, SenseNetResourceManager.Current.GetString(this.ContentType.DisplayName), this.Name, this.ContentType.Icon, this.Path, this.ContentHandler is IFolder);

            if (options == null)
                options = SerializationOptions.Default;

            var xmlCacheKey = FieldXmlCacheKey + options.GetHash();
            var fieldsXml = this.ContentHandler.GetCachedData(xmlCacheKey) as string;

            if (string.IsNullOrEmpty(fieldsXml))
            {
                using (var sw = new StringWriter())
                {
                    using (var xw = new XmlTextWriter(sw))
                    {
                        xw.WriteStartElement("Fields");
                        this.WriteFieldsData(xw, options);
                        xw.WriteEndElement();

                        fieldsXml = sw.ToString();

                        // insert into cache
                        if (this.ContentHandler is GenericContent)
                            this.ContentHandler.SetCachedData(xmlCacheKey, fieldsXml);
                    }
                }
            }

            // write fields xml
            writer.WriteRaw(fieldsXml);

            if (options == null || options.Actions == ActionSerializationOptions.All)
                base.WriteActions(writer, this.Path, Actions);
        }

        protected override void WriteXml(XmlWriter writer, string referenceMemberName, SerializationOptions options)
        {
            writer.WriteStartElement("Content");
            base.WriteHead(writer, this.ContentType.Name, this.Name, this.ContentType.Icon, this.Path, this.ContentHandler is IFolder);

            var xmlCacheKey = options == null ? FieldXmlCacheKey : FieldXmlCacheKey + options.GetHash();
            var fieldsXml = this.ContentHandler.GetCachedData(xmlCacheKey) as string;

            if (string.IsNullOrEmpty(fieldsXml))
            {
                using (var sw = new StringWriter())
                {
                    using (var xw = new XmlTextWriter(sw))
                    {
                        xw.WriteStartElement("Fields");
                        this.WriteFieldsData(xw, options);
                        xw.WriteEndElement();

                        fieldsXml = sw.ToString();

                        // insert into cache
                        if (this.ContentHandler is GenericContent)
                            this.ContentHandler.SetCachedData(xmlCacheKey, fieldsXml);
                    }
                }
            }

            // write fields xml
            writer.WriteRaw(fieldsXml);

            if (options == null || options.Actions == ActionSerializationOptions.All)
                base.WriteActions(writer, this.Path, Actions);

            if (!string.IsNullOrEmpty(referenceMemberName))
            {
                var folder = ContentHandler as IFolder;
                writer.WriteStartElement(referenceMemberName);
                WriteXml(this[referenceMemberName] as IEnumerable<Node>, writer, options);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        private void WriteFieldsData(XmlWriter writer, SerializationOptions options)
        {
            if (options == null)
                options = SerializationOptions.Default;
            switch (options.Fields)
            {
                case FieldSerializationOptions.All:
                    foreach (var field in this.Fields.Values)
                        WriteFieldData(field, writer);
                    return;
                case FieldSerializationOptions.Custom:
                    if (options.FieldNames == null)
                        return;
                    if (options.FieldNames.Count() == 0)
                        return;
                    foreach (var fieldName in options.FieldNames)
                    {
                        Field field;
                        if (this.Fields.TryGetValue(fieldName, out field))
                            WriteFieldData(field, writer);
                    }
                    return;
                case FieldSerializationOptions.None:
                    return;
                default:
                    throw new SnNotSupportedException("Unknown FieldSerializationOptions: " + options.Fields);
            }
        }
        private void WriteFieldData(Field field, XmlWriter writer)
        {
            if (field.Name == "Name" || (field.Name == "Versions" && !Security.HasPermission(Storage.Security.PermissionType.RecallOldVersion)))
                return;

            try
            {
                field.WriteXml(writer);
            }
            catch (SenseNetSecurityException)
            {
                // access denied to the field
            }
            catch (InvalidOperationException ex)
            {
                // access denied to a reference field...
                if (ex.InnerException is SenseNetSecurityException)
                    return;

                // unknown error
                SnLog.WriteException(ex);
            }
            catch (Exception ex)
            {
                // unknown error
                SnLog.WriteException(ex);
            }
        }

        // ================================================================================== Actions

        private IEnumerable<ActionBase> _actions;
        public IEnumerable<ActionBase> Actions
        {
            get
            {
                if (_actions == null)
                    _actions = GetActions();

                return _actions;
            }
        }

        /// <summary>
        /// Returns all conventional (non-virtual) actions available on the Content.
        /// </summary>
        /// <returns>An IEnumerable&lt;ActionBase&gt;</returns> 
        public IEnumerable<ActionBase> GetActions()
        {
            try
            {
                return ActionFramework.GetActions(this);
            }
            catch (InvalidContentActionException ex)
            {
                SnLog.WriteWarning(ex.Message, EventId.Indexing, properties: new Dictionary<string, object>
                {
                    { "Id", this.Id },
                    { "Path", this.Path },
                });
            }

            return new ActionBase[0];
        }
        /// <summary>
        /// Returns all conventional (non-virtual) actions available on the Content.
        /// </summary>
        /// <returns>An IEnumerable&lt;ActionBase&gt;</returns>
        [Obsolete("Use the Actions property instead")]
        public IEnumerable<ActionBase> GetContentActions()
        {
            return GetActions();
        }

        // =================================================================================== Runtime Content

        private Content(RuntimeContentHandler contentHandler, ContentType contentType)
        {
            _contentHandler = contentHandler;
            _contentType = contentType;
            _fields = new Dictionary<string, Field>();
            foreach (FieldSetting fieldSetting in contentType.FieldSettings)
            {
                Field field = Field.Create(this, fieldSetting);
                _fields.Add(field.Name, field);
            }
        }

        public static Content Create(object objectToEdit, string ctd)
        {
            if (objectToEdit == null)
                throw new ArgumentNullException("objectToEdit");
            ContentType contentType = ContentType.Create(objectToEdit.GetType(), ctd);
            if (contentType == null)
                throw new ApplicationException("Cannot create content from a " + objectToEdit.GetType().FullName);
            var node = new RuntimeContentHandler(objectToEdit, contentType);
            return new Content(node, contentType);
        }

        public class RuntimeContentHandler : Node
        {
            private Type _type;
            private object _object;
            public ContentType ContentType { get; private set; }

            public override bool IsContentType { get { return false; } }

            public override string Name
            {
                get
                {
                    return GetPropertySafely("Name") as string;
                }
                set
                {
                    SetPropertySafely("Name", value);
                }
            }

            public override string Path
            {
                get
                {
                    return "/Root/xxx";
                }
            }

            public override int ContentListId { get { return 0; } }
            public override Storage.Schema.ContentListType ContentListType { get { return null; } }
            public override int ContentListTypeId { get { return 0; } }
            public override int NodeTypeId { get { return 0; } }
            public override Storage.Schema.NodeType NodeType { get { return null; } }

            public override int CreatedById { get { return 1; } }
            public override Node CreatedBy { get { return User.Administrator; } set { } }
            public override DateTime CreationDate { get { return DateTime.UtcNow; } set { } }
            public override DateTime ModificationDate { get { return DateTime.UtcNow; } set { } }
            public override Node ModifiedBy { get { return User.Administrator; } set { } }
            public override int ModifiedById { get { return 1; } }
            public override Node VersionCreatedBy { get { return User.Administrator; } set { } }
            public override int VersionCreatedById { get { return 1; } }
            public override DateTime VersionCreationDate { get { return DateTime.UtcNow; } set { } }
            public override DateTime VersionModificationDate { get { return DateTime.UtcNow; } set { } }
            public override Node VersionModifiedBy { get { return User.Administrator; } set { } }
            public override int VersionModifiedById { get { return 1; } }
            public override int Index { get; set; }


            public override string DisplayName
            {
                get
                {
                    return GetPropertySafely("DisplayName") as string;
                }
                set
                {
                    SetPropertySafely("DisplayName", value);
                }
            }

            private bool _isNew = true;
            public override bool IsNew
            {
                get
                {
                    return _isNew;
                }
            }

            public void SetIsNew(bool isNew)
            {
                _isNew = isNew;
            }

            public RuntimeContentHandler(object objectToEdit, ContentType contentType)
                : base()
            {
                _object = objectToEdit;
                _type = _object.GetType();
                this.ContentType = contentType;
            }

            public override bool HasProperty(string name)
            {
                return _type.GetProperty(name) != null;
            }

            public override object GetPropertySafely(string name)
            {
                return _type.GetProperty(name) != null ? GetProperty(name) : null;
            }

            /// <summary>
            /// Sets the value to the specified property. Use it only if you want to hide the excetion if the field does not exist.
            /// </summary>
            /// <param name="name">Name of the property</param>
            /// <param name="value">New value</param>
            protected void SetPropertySafely(string name, object value)
            {
                if (_type.GetProperty(name) != null)
                    SetProperty(name, value);
            }

            public object GetProperty(string name)
            {
                var prop = _type.GetProperty(name);
                var getter = prop.GetGetMethod();
                var value = getter.Invoke(_object, null);
                return value;
            }
            public void SetProperty(string name, object value)
            {
                var prop = _type.GetProperty(name);
                var setter = prop.GetSetMethod();
                setter.Invoke(_object, new object[] { value });
            }

            public override void Save() { }
        }

        // ============================================================================= ICustomTypeDescriptor

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return new AttributeCollection(null);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return null;
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return null;
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return null;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return null;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return new EventDescriptorCollection(null);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return new EventDescriptorCollection(null);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(null);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return PropertyDescriptors ?? GetContentProperties();
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        // ----------------------------------------------------------- ICustomTypeDescriptor helpers

        private PropertyDescriptorCollection GetContentProperties()
        {
            var props = new List<PropertyDescriptor>();

            foreach (var field in this.Fields.Values)
            {
                var fs = FieldSetting.GetRoot(field.FieldSetting);

                props.Add(new FieldSettingPropertyDescriptor(field.Name, field.Name, fs));
                props.Add(new FieldSettingPropertyDescriptor(fs.BindingName, field.Name, fs));
            }

            return new PropertyDescriptorCollection(props.ToArray());
        }

        public static PropertyDescriptorCollection GetPropertyDescriptors(IEnumerable<string> fieldNames)
        {
            var props = new List<PropertyDescriptor>();

            if (fieldNames == null)
                return null;

            foreach (var fullName in fieldNames)
            {
                var fieldName = string.Empty;
                var fs = FieldSetting.GetFieldSettingFromFullName(fullName, out fieldName);
                var bindingName = fs == null ? FieldSetting.GetBindingNameFromFullName(fullName) : fs.BindingName;

                props.Add(new FieldSettingPropertyDescriptor(bindingName, fieldName, fs));
            }

            return new PropertyDescriptorCollection(props.ToArray());
        }

        public class FieldSettingPropertyDescriptor : PropertyDescriptor
        {
            private FieldSetting _fieldSetting;
            private readonly string _fieldName;

            public FieldSettingPropertyDescriptor(string bindingName, string fieldName, FieldSetting fieldSetting)
                : base(bindingName, null)
            {
                this._fieldSetting = FieldSetting.GetRoot(fieldSetting);
                this._fieldName = fieldName;
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override Type ComponentType
            {
                get { return typeof(Content); }
            }

            public override object GetValue(object component)
            {
                var content = component as Content;

                if (content == null)
                    throw new ArgumentException("Component must be a content!", "component");

                if (!content.Fields.ContainsKey(_fieldName))
                    return null;

                if (_fieldSetting == null && _fieldName.StartsWith("#"))
                {
                    // this is a contentlist field. We can find the
                    // appropriate field setting for it now, when we have
                    // the exact content list
                    var cl = content.ContentHandler.LoadContentList() as ContentList;

                    if (cl != null)
                    {
                        var listFs = cl.FieldSettings.FirstOrDefault(clfs => string.Compare(clfs.Name, _fieldName, StringComparison.InvariantCulture) == 0);
                        if (listFs != null)
                            _fieldSetting = listFs.GetEditable();
                    }
                }

                var fs = FieldSetting.GetRoot(content.Fields[_fieldName].FieldSetting);
                object result;

                if (_fieldSetting == null || fs == null)
                {
                    // we have not enough info for fullname check
                    result = content[_fieldName];
                }
                else
                {
                    // return the value only if fieldname refers to
                    // the same field (not just a field with the same name)
                    result = string.Compare(_fieldSetting.FullName, fs.FullName, StringComparison.InvariantCulture) != 0 ? null : content[_fieldName];
                }

                // format or change the value based on its type

                // CHOICE
                var sList = result as List<string>;
                if (sList != null)
                {
                    var chf = _fieldSetting as ChoiceFieldSetting;

                    if (chf != null)
                    {
                        result = new ChoiceOptionValueList<string>(sList, chf);
                    }
                    else
                    {
                        result = new StringValueList<string>(sList);
                    }

                    return result;
                }

                // REFERENCE
                var nodeList = result as List<Node>;
                if (nodeList != null)
                {
                    return new NodeValueList<Node>(nodeList);
                }

                // NUMBER
                if (result != null && content.Fields[_fieldName] is NumberField)
                {
                    if ((decimal)result == ActiveSchema.DecimalMinValue)
                        return null;
                }

                // INTEGER
                if (result != null && content.Fields[_fieldName] is IntegerField)
                {
                    if ((int)result == int.MinValue)
                        return null;
                }

                // HYPERLINK
                if (result != null && content.Fields[_fieldName] is HyperLinkField)
                {
                    var linkData = result as HyperLinkField.HyperlinkData;
                    if (linkData == null)
                        return null;

                    var sb = new StringBuilder();
                    sb.Append("<a");
                    if (linkData.Href != null)
                        sb.Append(" href=\"").Append(linkData.Href).Append("\"");
                    if (linkData.Target != null)
                        sb.Append(" target=\"").Append(linkData.Target).Append("\"");
                    if (linkData.Title != null)
                        sb.Append(" title=\"").Append(linkData.Title).Append("\"");
                    sb.Append(">");
                    sb.Append(linkData.Text ?? "");
                    sb.Append("</a>");
                    return sb.ToString();
                }

                return result;
            }

            public override bool IsReadOnly
            {
                get { return true; }
            }

            public override Type PropertyType
            {
                get { return _fieldSetting == null ? typeof(object) : _fieldSetting.FieldDataType; }
            }

            public override void ResetValue(object component)
            {

            }

            public override void SetValue(object component, object value)
            {

            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }

        public IEnumerable<string> GetFieldNamesInParentTable()
        {
            // Elevation: user permissions should not effect
            // field name collection (Parent may be unaccessible
            // for some users).
            using (new SystemAccount())
            {
                var content = this.ContentHandler;
                if (content.Parent != null)
                    content = content.Parent;
                return GetFieldNamesInTable(content);
            }
        }
        public IEnumerable<string> GetFieldNamesInTable()
        {
            return GetFieldNamesInTable(this.ContentHandler);
        }
        private IEnumerable<string> GetFieldNamesInTable(Node content)
        {
            var allNames = ContentTypeManager.Current.AllFieldNames;
            var gc = content as GenericContent;
            if (gc == null)
            {
                if (content is ContentType)
                    return ContentType.GetByName("ContentType").FieldSettings.Select(f => f.Name).ToArray();
                return allNames;
            }

            var types = gc.GetAllowedChildTypes();
            if (types.Count() == 0)
            {
                return allNames;
            }

            var bits = new int[this.ContentType.FieldBits.Length];
            foreach (var ct in types)
            {
                var ctbits = ct.FieldBits;
                for (int i = 0; i < bits.Length; i++)
                    bits[i] |= ctbits[i];
            }

            var names = new List<string>();
            var index = 0;
            foreach (var x in bits)
            {
                var mask = 1;
                for (int i = 0; i < sizeof(int) * 8; i++)
                {
                    if ((x & mask) != 0)
                        names.Add(allNames[index]);
                    mask = mask << 1;
                    index++;
                }
            }
            var list = (ContentList)ContentHandler.LoadContentList();
            if (list != null)
            {
                foreach (var field in list.GetAvailableFields())
                    if (field.Name.StartsWith("#"))
                        names.Add(field.Name);
            }

            var dynamicContentTypes = types.Where(x => typeof(ISupportsDynamicFields).IsAssignableFrom(TypeResolver.GetType(x.HandlerName))).ToArray();
            if (dynamicContentTypes.Length > 0)
            {
                var results = ContentQuery_NEW.Query(SafeQueries.InFolderAndTypeIs,
                    new QuerySettings { EnableAutofilters = FilterStatus.Enabled, QueryExecutionMode = QueryExecutionMode.Quick },
                    content.Path, dynamicContentTypes);
                foreach (var meta in results.Nodes.Cast<ISupportsDynamicFields>().Select(x => x.GetDynamicFieldMetadata()).Where(x => x != null))
                    names.AddRange(meta.Keys.Where(x => !names.Contains(x)).Distinct());
            }


            return names;
        }

        // =========================================================================== Aspect API
        public void AddAspects(params Aspect[] aspects)
        {
            ContentHandler.AddReferences(GenericContent.ASPECTS, aspects, true);
            RebuildAspectFields();
        }
        public void AddAspects(params string[] aspectPaths)
        {
            AddAspects(aspectPaths.Select(p => NodeHead.Get(p).Id).ToArray());
            RebuildAspectFields();
        }
        public void AddAspects(params int[] aspectIds)
        {
            ContentHandler.AddReferences(GenericContent.ASPECTS, new NodeList<Aspect>(aspectIds), true);
            RebuildAspectFields();
        }
        public void RemoveAspects(params Aspect[] aspects)
        {
            foreach (var aspect in aspects)
                ContentHandler.RemoveReference(GenericContent.ASPECTS, aspect);
            RebuildAspectFields();
        }
        public void RemoveAspects(params string[] aspectPaths)
        {
            foreach (var aspectPath in aspectPaths)
            {
                var aspect = Aspect.LoadAspectByPathOrName(aspectPath);
                if (aspect != null)
                    ContentHandler.RemoveReference(GenericContent.ASPECTS, aspect);
            }
            RebuildAspectFields();
        }
        public void RemoveAspects(params int[] aspectIds)
        {
            foreach (var aspectId in aspectIds)
                ContentHandler.RemoveReference(GenericContent.ASPECTS, Node.Load<Aspect>(aspectId));
            RebuildAspectFields();
        }
        public void RemoveAllAspects()
        {
            ContentHandler.ClearReference(GenericContent.ASPECTS);
            RebuildAspectFields();
        }

        private void RebuildAspectFields()
        {
            if (AspectFields != null)
            {
                foreach (var key in AspectFields.Keys)
                    Fields.Remove(key);
                AspectFields.Clear();
            }
            BuildAspectFields();
        }
        private void BuildAspectFields()
        {
            if (ContentHandler.IsHeadOnly)
                return;

            try
            {
                var gc = this.ContentHandler as GenericContent;
                if (gc != null)
                {
                    var aspects = gc.GetPropertySafely("Aspects") as IEnumerable<Node>;
                    if (aspects != null)
                    {
                        if (aspects.Count() > 0)
                        {
                            AspectFields = new Dictionary<string, Field>();
                            foreach (Aspect aspect in aspects)
                            {
                                foreach (var fieldSetting in aspect.FieldSettings)
                                {
                                    var field = Field.Create(this, fieldSetting, aspect);
                                    field.SetDefaultValue();
                                    if (!_fields.ContainsKey(field.Name))
                                    {
                                        _fields.Add(field.Name, field);
                                        AspectFields.Add(field.Name, field);
                                    }
                                }
                            }
                            var xmlSrc = gc.AspectData;
                            if (!string.IsNullOrEmpty(xmlSrc))
                            {
                                var xml = new XmlDocument();
                                xml.LoadXml(xmlSrc);
                                foreach (XmlElement element in xml.DocumentElement.ChildNodes)
                                {
                                    Field field;
                                    var name = element.Name;
                                    if (AspectFields.TryGetValue(name, out field))
                                    {
                                        try
                                        {
                                            field.Import(element);
                                        }
                                        catch
                                        {
                                            //TODO: Can't parse field - value is treated as null
                                            // 1) persist field type into AspectData
                                            // 2) check here if field type has changed or not
                                            // 3) if field type changed, it means we can safely treat the value as null
                                            // 4) if field type didn't change but the field still can't parse the XML, it means there is a programmer error in the field
                                            // 4/a) set field value to null and save content
                                            // 4/b) throw exception
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Error during creating an aspect's fields.", exc);
            }
        }
    }

    public class StringValueList<T> : List<string>
    {
        public StringValueList() : base() { }
        public StringValueList(IEnumerable<string> list) : base(list) { }

        public override string ToString()
        {
            return string.Join(", ", this.ToArray());
        }
    }

    public class NodeValueList<T> : List<Node>
    {
        public NodeValueList() : base() { }
        public NodeValueList(IEnumerable<Node> list) : base(list) { }

        public override string ToString()
        {
            return string.Join(", ", (from node in this
                                      select node.ToString()).ToArray());
        }
    }

    public class ChoiceOptionValueList<T> : List<string>
    {
        private readonly ChoiceFieldSetting _fieldSetting;
        private readonly bool _displayValue;

        public ChoiceOptionValueList()
            : base()
        {
        }

        public ChoiceOptionValueList(IEnumerable<string> list, ChoiceFieldSetting fieldSetting) : this(list, fieldSetting, false) { }

        public ChoiceOptionValueList(IEnumerable<string> list, ChoiceFieldSetting fieldSetting, bool displayValue)
            : base(list)
        {
            _fieldSetting = fieldSetting;
            _displayValue = displayValue;
        }

        public override string ToString()
        {
            if (_fieldSetting == null)
                return string.Empty;

            var resultOptions = _displayValue ?
                (from opt in _fieldSetting.Options
                 where this.Contains(opt.Value)
                 select opt.Value).ToList() :
                (from opt in _fieldSetting.Options
                 where this.Contains(opt.Value)
                 select opt.Text).ToList();

            if (_fieldSetting.AllowExtraValue.HasValue && _fieldSetting.AllowExtraValue.Value)
            {
                resultOptions.AddRange(from str in this
                                       where str.StartsWith(ChoiceField.EXTRAVALUEPREFIX)
                                       select str.Substring(ChoiceField.EXTRAVALUEPREFIX.Length));
            }

            return string.Join(", ", resultOptions.ToArray());
        }
    }
}

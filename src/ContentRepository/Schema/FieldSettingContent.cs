using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema
{
    /// <summary>
    /// A Content handler that represents a <see cref="Schema.FieldSetting"/> as a Content.
    /// </summary>
    [ContentHandler]
    public class FieldSettingContent : GenericContent, ISupportsDynamicFields
    {
        /// <summary>
        /// Gets or sets the represented <see cref="Schema.FieldSetting"/> instance.
        /// </summary>
        public FieldSetting FieldSetting { get; set; }
        /// <summary>
        /// Gets or sets the owner <see cref="ContentRepository.ContentList"/> of the 
        /// represented <see cref="Schema.FieldSetting"/> instance.
        /// </summary>
        public ContentList ContentList { get; set; }
        /// <summary>
        /// Gets or sets the owner <see cref="Schema.ContentType"/> of the 
        /// represented <see cref="Schema.FieldSetting"/> instance.
        /// </summary>
        public new ContentType ContentType { get; set; } // visibility changed

        private bool _isNew = true;

        /// <summary>
        /// Gets or sets a value that is true if a new field should be added to the default view
        /// of the containing Content List.
        /// </summary>
        public bool AddToDefaultView { get; set; }

        // ================================================================= Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSettingContent"/> class from the
        /// specified <see cref="Schema.FieldSetting"/> for the given <see cref="ContentRepository.ContentList"/>.
        /// </summary>
        /// <param name="fieldSetting">The represented <see cref="Schema.FieldSetting"/> instance.</param>
        /// <param name="contentList">The <see cref="ContentRepository.ContentList"/> that is owner of the 
        /// represented <see cref="Schema.FieldSetting"/> instance.</param>
        public FieldSettingContent(FieldSetting fieldSetting, ContentList contentList) :
            this(contentList ?? Repository.Root as Node, GetNodeTypeName(fieldSetting))
        {
            this.FieldSetting = fieldSetting;
            this.ContentList = contentList;

            this.AddToDefaultView = false;
            this.Name = this.FieldSetting.Name;
            this._isNew = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSettingContent"/> class from the
        /// specified <see cref="Schema.FieldSetting"/> for the given <see cref="Schema.ContentType"/>.
        /// </summary>
        /// <param name="fieldSetting">The represented <see cref="Schema.FieldSetting"/> instance.</param>
        /// <param name="contentType">The <see cref="Schema.ContentType"/> that is owner of the 
        /// represented <see cref="Schema.FieldSetting"/> instance.</param>
        public FieldSettingContent(FieldSetting fieldSetting, ContentType contentType) :
            this(Repository.Root, GetNodeTypeName(fieldSetting))
        {
            this.FieldSetting = fieldSetting;
            this.ContentType = contentType;

            this.Name = this.FieldSetting.Name;
            this._isNew = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSettingContent"/> class.
        /// Do not use this constructor directly in your code.
        /// </summary>
        /// <param name="parent">The parent.</param>
        protected FieldSettingContent(Node parent) : base(parent, "FieldSettingContent")
        {
            base.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSettingContent"/> class.
        /// Do not use this constructor directly in your code.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public FieldSettingContent(Node parent, string nodeTypeName): base(parent, nodeTypeName)
        {
            this.ContentList = parent as ContentList;

            var fsType = (from fst in TypeResolver.GetTypesByBaseType(typeof(FieldSetting))
                          where fst.Name.CompareTo(nodeTypeName) == 0
                          select fst).FirstOrDefault();

            if (fsType == null)
                return;

            var typeName = fsType.FullName;
            if (typeName.EndsWith("Setting"))
                typeName = typeName.Remove(typeName.LastIndexOf("Setting"));

            this.FieldSetting = Activator.CreateInstance(fsType) as FieldSetting;

            if (this.FieldSetting == null)
                return;

            this.FieldSetting.ShortName = FieldManager.GetShortName(typeName);
            this.AddToDefaultView = true;

            base.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSettingContent"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected FieldSettingContent(NodeToken nt): base(nt)
        {
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            // do nothing. base is called back in constructor of this
        }

        // ====================================================================== Properties

        /// <summary>
        /// Gets or sets the order of the field based on the represented <see cref="Schema.FieldSetting"/> instance.
        /// If it is currently null, returns null. Assign is ineffective in that case.
        /// </summary>
        public int? FieldIndex
        {
            get
            {
                return this.FieldSetting != null ? this.FieldSetting.FieldIndex : null;
            }
            set
            {
                if (this.FieldSetting != null)
                    this.FieldSetting.FieldIndex = value;
            }
        }

        // ================================================================= Node methods

        /// <summary>
        /// Saves the <see cref="Schema.FieldSetting"/> data to the Content List definition xml. This method
        /// does not call the base Save implementation, no standalone field setting content is saved into the
        /// Content Repository.
        /// </summary>
        public override void Save(SavingMode mode)
        {
            SaveFieldSetting();
        }

        /// <summary>
        /// Deletes the <see cref="Schema.FieldSetting"/> data from the Content List definition xml. This method
        /// does not call the base Delete implementation.
        /// </summary>
        public override void Delete()
        {
            // remove column from views
            var ivm = Providers.Instance.GetProvider<IViewManager>("ViewManager");
            ivm?.RemoveFieldFromViews(this.FieldSetting, this.ContentList);

            this.ContentList.DeleteField(this.FieldSetting);
        }

        private void SaveFieldSetting()
        {
            var fsName = this.Name;
            if (!fsName.StartsWith("#"))
                fsName = string.Concat("#", fsName);

            if (this._isNew)
            {
                // set field index default to int.Max to place 
                // the new field at the end of the field list
                this.FieldIndex = int.MaxValue;
            }

            if (this._isNew && this.ContentList.FieldSettings.Any(fs => fs.Name.CompareTo(fsName) == 0))
            {
                // field already exists with this name
                throw new InvalidOperationException(SR.GetString("FieldEditor", "FieldError_FieldExists"));
            }

            var ivm = Providers.Instance.GetProvider<IViewManager>("ViewManager");

            if (string.CompareOrdinal(this.FieldSetting.Name, fsName) != 0)
            {
                // field setting was renamed, remove the old one
                // remove column from views
                ivm?.RemoveFieldFromViews(this.FieldSetting, this.ContentList);

                this.ContentList.DeleteField(this.FieldSetting);

                this.FieldSetting.Name = fsName;
            }

            this.ContentList.AddOrUpdateField(this.FieldSetting);

            if (!this._isNew || !this.AddToDefaultView) 
                return;

            // add to default view
            ivm?.AddToDefaultView(this.FieldSetting, this.ContentList);
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            var found = false;
            var val = this.FieldSetting.GetProperty(name, out found);

            if (found)
                return val;

            switch (name)
            {
                case "Name": return this.Name;
                case FieldSetting.AddToDefaultViewName: return AddToDefaultView;
            }

            var type = this.FieldSetting.GetType();

            foreach (var propertyInfo in type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public))
            {
                if (propertyInfo.Name.CompareTo(name) != 0)
                    continue;

                return !propertyInfo.CanRead ? null : propertyInfo.GetValue(this.FieldSetting, null);
            }

            return base.GetProperty(name);
        }

        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            if (this.FieldSetting.SetProperty(name, value))
                return;

            switch (name)
            {
                case "Name": 
                    this.Name = value as string;
                    return;
                case FieldSetting.AddToDefaultViewName: 
                    AddToDefaultView = (bool)value;
                    return;
            }

            var type = this.FieldSetting.GetType();
            var found = false;

            foreach (var propertyInfo in type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public))
            {
                if (propertyInfo.Name.CompareTo(name) != 0) 
                    continue;

                found = true;

                if (!propertyInfo.CanWrite)
                    break;

                propertyInfo.SetValue(this.FieldSetting, value, null);
                break;
            }

            if (!found)
                base.SetProperty(name, value);
        }

        #region ISupportsDynamicFields Members

        /// <inheritdoc />
        public IDictionary<string, FieldMetadata> GetDynamicFieldMetadata()
        {
            return this.FieldSetting.GetFieldMetadata();
        }

        /// <inheritdoc />
        object ISupportsDynamicFields.GetProperty(string name)
        {
            return this.GetProperty(name);
        }

        /// <inheritdoc />
        void ISupportsDynamicFields.SetProperty(string name, object value)
        {
            this.SetProperty(name, value);
        }

        /// <inheritdoc />
        bool ISupportsDynamicFields.IsNewContent
        {
            get { return this._isNew; }
        }

        /// <inheritdoc />
        void ISupportsDynamicFields.ResetDynamicFields()        
        { 
            // do nothing
        }

        #endregion

        // ================================================================= Helper methods

        private static string GetNodeTypeName(FieldSetting fieldSetting)
        {
            if (fieldSetting == null)
                throw new ArgumentNullException("fieldSetting");

            var nt = fieldSetting.GetType();

            while (ActiveSchema.NodeTypes[nt.Name] == null)
            {
                nt = nt.BaseType;
            }

            return nt.Name;
        }
    }
}
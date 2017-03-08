using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Schema
{
    [ContentHandler]
    public class FieldSettingContent : GenericContent, ISupportsDynamicFields
    {
        public FieldSetting FieldSetting { get; set; }
        public ContentList ContentList { get; set; }
        // visibility changed
        public new ContentType ContentType { get; set; }

        private bool _isNew = true;

        public bool AddToDefaultView { get; set; }

        // ================================================================= Constructors

        public FieldSettingContent(FieldSetting fieldSetting, ContentList contentList) :
            this(contentList ?? Repository.Root as Node, GetNodeTypeName(fieldSetting))
        {
            this.FieldSetting = fieldSetting;
            this.ContentList = contentList;

            this.AddToDefaultView = false;
            this.Name = this.FieldSetting.Name;
            this._isNew = false;
        }

        public FieldSettingContent(FieldSetting fieldSetting, ContentType contentType) :
            this(Repository.Root, GetNodeTypeName(fieldSetting))
        {
            this.FieldSetting = fieldSetting;
            this.ContentType = contentType;

            this.Name = this.FieldSetting.Name;
            this._isNew = false;
        }

        protected FieldSettingContent(Node parent) : base(parent, "FieldSettingContent")
        {
            base.Initialize();
        }

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

        protected FieldSettingContent(NodeToken nt): base(nt)
        {
        }

        protected override void Initialize()
        {
            // do nothing. base is called back in constructor of this
        }

        // ====================================================================== Properties

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

        public override void Save(SavingMode mode)
        {
            SaveFieldSetting();
        }

        public override void Delete()
        {
            // remove column from views
            var ivm = TypeHandler.ResolveNamedType<IViewManager>("ViewManager");
            if (ivm != null)
                ivm.RemoveFieldFromViews(this.FieldSetting, this.ContentList);

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
                throw new InvalidOperationException(HttpContext.GetGlobalResourceObject("FieldEditor", "FieldError_FieldExists") as string);
            }

            var ivm = TypeHandler.ResolveNamedType<IViewManager>("ViewManager");

            if (string.CompareOrdinal(this.FieldSetting.Name, fsName) != 0)
            {
                // field setting was renamed, remove the old one
                // remove column from views
                if (ivm != null)
                    ivm.RemoveFieldFromViews(this.FieldSetting, this.ContentList);

                this.ContentList.DeleteField(this.FieldSetting);

                this.FieldSetting.Name = fsName;
            }

            this.ContentList.AddOrUpdateField(this.FieldSetting);

            if (!this._isNew || !this.AddToDefaultView) 
                return;

            // add to default view
            if (ivm != null)
                ivm.AddToDefaultView(this.FieldSetting, this.ContentList);
        }

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

        public IDictionary<string, FieldMetadata> GetDynamicFieldMetadata()
        {
            return this.FieldSetting.GetFieldMetadata();
        }

        object ISupportsDynamicFields.GetProperty(string name)
        {
            return this.GetProperty(name);
        }

        void ISupportsDynamicFields.SetProperty(string name, object value)
        {
            this.SetProperty(name, value);
        }

        bool ISupportsDynamicFields.IsNewContent
        {
            get { return this._isNew; }
        }

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
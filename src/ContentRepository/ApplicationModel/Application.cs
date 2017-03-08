using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using System;
using System.Web;
using System.Linq;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ApplicationModel
{
    public enum IncludeBackUrlMode { Default = 0, True = 1, False = 2 }

    [ContentHandler]
    public class Application : GenericContent
    {
        public Application(Node parent) : this(parent, null) { }
        public Application(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { Init(); }
        protected Application(NodeToken nt) : base(nt) { Init(); }
        private void Init()
        {
            IsOverride = this.NodeType.IsInstaceOfOrDerivedFrom("ApplicationOverride");
        }

        private static readonly string[] EXCLUDED_OVERRIDE_PROPERTIES = { "Name", "Path", "Id", "NodeType", "ContentListId", "ContentListType", "Parent", "IsModified", "IsDeleted", "Version","VersionId", 
                                                                          "CreationDate", "ModificationDate", "CreatedBy", "ModifiedBy", "VersionCreationDate", "VersionModificationDate", "VersionCreatedById", 
                                                                          "VersionModifiedById", "Locked", "Lock","LockedById","LockedBy", "LockType", "LockTimeout", "LockDate", "LockToken", "LastLockUpdate", "Security",
                                                                          "SavingState", "ChangedData", "OwnerId"
                                                                        };
        //TODO: IsSystem

        public Application Override { get; set; }
        public bool IsOverride { get; private set; }

        private object GetOverrideProperty(string name)
        {
            if (string.IsNullOrEmpty(name) || EXCLUDED_OVERRIDE_PROPERTIES.Contains(name))
                return null;

            return Override != null && Override.HasProperty(name) ? Override.GetProperty(name) : null;
        }

        private T GetOverriddenProperty<T>(string name)
        {
            var o = GetOverrideProperty(name);
            if (o != null && typeof(T) == typeof(string))
            {
                var os = o as string;
                if (os == null)
                {
                    // if 'o' is of a different type - e.g. an Enum
                    o = Override.GetPropertySafely(name) as string;
                }
                else
                {
                    // in case of string.Empty
                    if (string.IsNullOrEmpty(os))
                        o = null;
                }
            }

            return (o == null) ? (T)base.GetProperty(name) : (T)o;
        }

        public const string APPNAME = "AppName";
        [RepositoryProperty(APPNAME, RepositoryDataType.String)]
        public string AppName
        {
            get { return GetProperty<string>(APPNAME); }
        }

        public override string DisplayName
        {
            get
            {
                if (Override != null)
                {
                    var overriddenDisplayName = Override.DisplayName;
                    if (!String.IsNullOrEmpty(overriddenDisplayName))
                        return overriddenDisplayName;
                }
                var displayName = (string)GetStoredValue("DisplayName");
                if (IsOverride)
                    return displayName;
                return base.DisplayName;
            }
            set { base.DisplayName = (value == this.Name) ? null : value; }
        }

        public override string Description
        {
            get { return this.GetOverriddenProperty<string>("Description"); }
            set { base.Description = value; }
        }

        [RepositoryProperty("Disabled", RepositoryDataType.Int)]
        public bool Disabled
        {
            get
            {
                // this property cannot be overridden
                return this.GetProperty<int>("Disabled") != 0;
            }
            set { this["Disabled"] = value ? 1 : 0; }
        }

        [RepositoryProperty("IsModal", RepositoryDataType.Int)]
        public bool IsModal
        {
            get
            {
                // this property cannot be overridden
                return this.GetProperty<int>("IsModal") != 0;
            }
            set { this["IsModal"] = value ? 1 : 0; }
        }

        [RepositoryProperty("Clear", RepositoryDataType.Int)]
        public bool Clear
        {
            get
            {
                // this property cannot be overridden
                return this.GetProperty<int>("Clear") != 0;
            }
            set { this["Clear"] = value ? 1 : 0; }
        }

        [RepositoryProperty("Scenario", RepositoryDataType.String)]
        public virtual string Scenario
        {
            get { return this.GetOverriddenProperty<string>("Scenario"); }
            set { this["Scenario"] = value; }
        }

        public const string ACTIONTYPENAME = "ActionTypeName";
        [RepositoryProperty(ACTIONTYPENAME, RepositoryDataType.String)]
        public virtual string ActionTypeName
        {
            get { return this.GetOverriddenProperty<string>(ACTIONTYPENAME); }
            set { this[ACTIONTYPENAME] = value; }
        }

        [RepositoryProperty("StoredIcon", RepositoryDataType.String)]
        private string StoredIcon
        {
            get { return this.GetOverriddenProperty<string>("StoredIcon"); }
            set { base.SetProperty("StoredIcon", value); }
        }
        public override string Icon
        {
            get
            {
                var icon = Override != null ? Override.StoredIcon : null;
                if (!string.IsNullOrEmpty(icon))
                    return icon;
                icon = this.StoredIcon;
                if (!string.IsNullOrEmpty(icon))
                    return icon;
                return this.ContentType.Icon;
            }
            set { this.StoredIcon = value == this.ContentType.Icon ? null : value; }
        }

        [RepositoryProperty("StyleHint", RepositoryDataType.String)]
        public virtual string StyleHint
        {
            get { return this.GetOverriddenProperty<string>("StyleHint"); }
            set { this["StyleHint"] = value; }
        }

        public const string REQUIREDPERMISSIONS = "RequiredPermissions";
        [RepositoryProperty(REQUIREDPERMISSIONS, RepositoryDataType.String)]
        public virtual IEnumerable<PermissionType> RequiredPermissions
        {
            get
            {
                var value = GetOverriddenProperty<string>(REQUIREDPERMISSIONS);
                var result = SenseNet.ContentRepository.Fields.PermissionChoiceField.ConvertToPermissionTypes(value);
                return result;
            }
            set { this[REQUIREDPERMISSIONS] = SenseNet.ContentRepository.Fields.PermissionChoiceField.ConvertToMask(value); }
        }

        [RepositoryProperty("DeepPermissionCheck", RepositoryDataType.Int)]
        public virtual bool DeepPermissionCheck
        {
            get
            {
                // bool properties are not so simple in this scenario: we
                // get a bool value here, through the override's property 
                // accessor, or an int value from the db
                var o = GetOverrideProperty("DeepPermissionCheck");

                // this can not be null if we have an override - this is the
                // drawback of the int type - it always has a value
                if (o != null)
                    return (bool)o;

                return this.GetProperty<int>("DeepPermissionCheck") != 0;
            }
            set { this["DeepPermissionCheck"] = value ? 1 : 0; }
        }

        public const string INCLUDEBACKURL = "IncludeBackUrl";
        [RepositoryProperty(INCLUDEBACKURL, RepositoryDataType.String)]
        public IncludeBackUrlMode IncludeBackUrl
        {
            get
            {
                var result = IncludeBackUrlMode.Default;
                var enumVal = base.GetProperty<string>(INCLUDEBACKURL);
                if (string.IsNullOrEmpty(enumVal))
                    return result;

                Enum.TryParse(enumVal, false, out result);

                return result;
            }
            set
            {
                this[INCLUDEBACKURL] = Enum.GetName(typeof(IncludeBackUrlMode), value);
            }
        }

        public const string CACHECONTROL = "CacheControl";
        [RepositoryProperty(CACHECONTROL, RepositoryDataType.String)]
        public string CacheControl
        {
            get { return this.GetOverriddenProperty<string>(CACHECONTROL); }
            set { this[CACHECONTROL] = value; }
        }

        public HttpCacheability? CacheControlEnumValue
        {
            get
            {
                var strprop = this.CacheControl;
                if (string.IsNullOrEmpty(strprop) || strprop == "Nondefined")
                    return null;

                return (HttpCacheability)Enum.Parse(typeof(HttpCacheability), strprop, true);
            }
            set { this.CacheControl = value.HasValue ? value.ToString() : "Nondefined"; }
        }

        public const string MAXAGE = "MaxAge";
        [RepositoryProperty(MAXAGE, RepositoryDataType.String)]
        public virtual string MaxAge
        {
            get { return this.GetOverriddenProperty<string>(MAXAGE); }
            set { this[MAXAGE] = value; }
        }

        public const string CUSTOMURLPARAMETERS = "CustomUrlParameters";
        [RepositoryProperty(CUSTOMURLPARAMETERS, RepositoryDataType.String)]
        public string CustomUrlParameters
        {
            get { return this.GetOverriddenProperty<string>(CUSTOMURLPARAMETERS); }
            set { this[CUSTOMURLPARAMETERS] = value; }
        }

        private IDictionary<string, object> _customParams;
        private object _customParamObject = new object();
        public IDictionary<string, object> CustomParameters
        {
            get
            {
                if (_customParams == null)
                {
                    lock (_customParamObject)
                    {
                        if (_customParams == null)
                        {
                            var localDict = new Dictionary<string, object>();

                            if (!string.IsNullOrEmpty(CustomUrlParameters))
                            {
                                var cups = CustomUrlParameters.Split(new[] { "&", ";" }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (var customParam in cups)
                                {
                                    var paramAndValue = customParam.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                                    if (paramAndValue.Length == 0)
                                        continue;

                                    var key = paramAndValue[0];
                                    var val = paramAndValue.Length > 1 ? paramAndValue[1] : null;

                                    if (string.IsNullOrEmpty(key))
                                        continue;

                                    if (localDict.ContainsKey(key))
                                        localDict[key] = val;
                                    else
                                        localDict.Add(key, val);
                                }
                            }

                            _customParams = localDict;
                        }
                    }
                }

                return _customParams;
            }
        }

        public int? NumericMaxAge
        {
            get
            {
                var strprop = this.MaxAge;
                if (string.IsNullOrEmpty(strprop))
                    return null;

                int val;
                if (Int32.TryParse(strprop, out val))
                    return val;

                return null;
            }
            set { this.MaxAge = !value.HasValue ? null : value.ToString(); }
        }


        public List<string> ScenarioList
        {
            get
            {
                var scl = new List<string>();
                var sc = this.Scenario;

                if (!string.IsNullOrEmpty(sc))
                    scl.AddRange(sc.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(st => st.Trim()));

                return scl;
            }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case APPNAME:
                    return this.AppName;
                case "DisplayName":
                    return this.DisplayName;
                case "Scenario":
                    return this.Scenario;
                case "Disabled":
                    return this.Disabled;
                case "IsModal":
                    return this.IsModal;
                case "Clear":
                    return this.Clear;
                case "Icon":
                    return this.Icon;
                case "StyleHint":
                    return this.StyleHint;
                case REQUIREDPERMISSIONS:
                    return this.RequiredPermissions;
                case "DeepPermissionCheck":
                    return this.DeepPermissionCheck;
                case INCLUDEBACKURL:
                    return this.IncludeBackUrl;
                case CUSTOMURLPARAMETERS:
                    return this.CustomUrlParameters;
                case CACHECONTROL:
                    return this.CacheControl;
                case MAXAGE:
                    return this.MaxAge;
                case ACTIONTYPENAME:
                    return this.ActionTypeName;
                default:
                    return GetOverrideProperty(name) ?? base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "DisplayName":
                    this.DisplayName = (string)value;
                    break;
                case "Scenario":
                    this.Scenario = (string)value;
                    break;
                case "Disabled":
                    this.Disabled = (bool)value;
                    break;
                case "IsModal":
                    this.IsModal = (bool)value;
                    break;
                case "Clear":
                    this.Clear = (bool)value;
                    break;
                case "Icon":
                    this.Icon = (string)value;
                    break;
                case "StyleHint":
                    this.StyleHint = (string)value;
                    break;
                case REQUIREDPERMISSIONS:
                    this.RequiredPermissions = (IEnumerable<PermissionType>)value;
                    break;
                case "DeepPermissionCheck":
                    this.DeepPermissionCheck = (bool)value;
                    break;
                case INCLUDEBACKURL:
                    this.IncludeBackUrl = (IncludeBackUrlMode)value;
                    break;
                case CUSTOMURLPARAMETERS:
                    this.CustomUrlParameters = (string)value;
                    break;
                case CACHECONTROL:
                    this.CacheControl = (string)value;
                    break;
                case MAXAGE:
                    this.MaxAge = (string)value;
                    break;
                case ACTIONTYPENAME:
                    this.ActionTypeName = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }


        public override void Save(NodeSaveSettings settings)
        {
            var appParent = Parent as Application;

            var appName = appParent != null ? appParent.AppName : Name.Split('.')[0];

            if (this.AppName != appName)
                this.SetProperty(APPNAME, appName);

            base.Save(settings);
        }

        // ================================================================== Action framework

        public ActionBase CreateAction(Content context, string backUrl, object parameters)
        {
            try
            {
                return ActionFactory.CreateAction(GetActionTypeName(), this, context, backUrl, parameters);
            }
            catch (InvalidOperationException)
            {
                // not enough permissions to access actiontype name, return null
            }

            return null;
        }

        protected virtual string GetActionTypeName()
        {
            return this.ActionTypeName;
        }
        protected virtual Type GetActionType()
        {
            return typeof(ActionBase);
        }

        [Obsolete("Do not use")]
        protected string GetNameBase()
        {
            return Name.Split('.').FirstOrDefault();
        }
    }
}

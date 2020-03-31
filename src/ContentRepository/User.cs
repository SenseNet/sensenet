using System;
using System.Linq;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Security.ADSync;
using System.Collections.Generic;
using System.Diagnostics;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Fields;
using System.Security.Principal;
using SenseNet.Search;
using System.Xml.Serialization;
using System.IO;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Search.Querying;
using SenseNet.ContentRepository.Setting;
using SenseNet.Search.Querying;
using SenseNet.Security;
using SenseNet.Tools;
using Retrier = SenseNet.ContentRepository.Storage.Retrier;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// A content handler that encapsulates metadata and functionality of a sensenet user.
    /// </summary>
    [ContentHandler]
    public class User : GenericContent, IUser, IADSyncable, SenseNet.Security.ISecurityUser
    {
        private const string Profiles = "Profiles";

        /// <summary>
        /// Gets the Administrator user.
        /// Note that always returns a new instance.
        /// </summary>
        public static User Administrator
        {
            get
            {
                AccessProvider.ChangeToSystemAccount();
                User admin = Node.LoadNode(Identifiers.AdministratorUserId) as User;
                AccessProvider.RestoreOriginalUser();
                if (admin == null)
                    throw new ApplicationException("Administrator cannot be found.");
                return admin;
            }
        }

        /// <summary>
        /// Gets the Visitor user.
        /// </summary>
        public static User Visitor => SystemAccount.Execute(() => Load<User>(Identifiers.VisitorUserId));
        public static User DefaultUser => SystemAccount.Execute(() => Load<User>(AccessProvider.Current.DefaultUserId));

        private static User _somebody;
        private static object _somebodyLock = new object();
        /// <summary>
        /// Gets the Somebody user. This user is returned every time the logged-in user 
        /// is not allowed to access another user in the Content Repository.
        /// </summary>
        public static User Somebody
        {
            get
            {
                if (_somebody == null)
                {
                    lock (_somebodyLock)
                    {
                        if (_somebody == null)
                        {
                            using (new SystemAccount())
                            {
                                var somebody = Node.LoadNode(Identifiers.SomebodyUserId);
                                _somebody = somebody as User;
                            }
                        }
                    }
                }
                return _somebody;
            }
        }

        /// <summary>
        /// Gets or sets the current user. This can be a system administrator in case the caller is running
        /// inside a SystemAccount code block. To get the real logged in user, please use the LoggedInUser property.
        /// </summary>
        public static IUser Current
        {
            get
            {
                return AccessProvider.Current.GetCurrentUser();
            }
            set // [Explicit SignIn]
            {
                if (value == null)
                    throw new ArgumentNullException("value"); // Logout: set User.Visitor rather than null
                if (value.Id == 0)
                    throw new SenseNetSecurityException("Cannot log in with a non-saved (non-existing) user.");

                AccessProvider.Current.SetCurrentUser(value);
            }
        }

        /// <summary>
        /// Gets the actual logged-in user.
        /// </summary>
        public static IUser LoggedInUser
        {
            get { return AccessProvider.Current.GetOriginalUser(); }
        }

        private static readonly string[] PropertyNamesForCheckUniqueness = new[] { "Path", "Name", "LoginName" };

        private string _password;
        private bool _syncObject = true;


        private WindowsIdentity _windowsIdentity;
        /// <summary>
        /// Gets or sets the <see cref="WindowsIdentity"/> of the represented user if she is identified with windows authentication.
        /// </summary>
        public WindowsIdentity WindowsIdentity
        {
            get { return _windowsIdentity; }
            set { _windowsIdentity = value; }
        }

        private bool _inactivating;
        /// <inheritdoc />
        /// <remarks>Persisted as <see cref="RepositoryDataType.Int"/>.</remarks>
        [RepositoryProperty("Enabled", RepositoryDataType.Int)]
        public bool Enabled
        {
            get { return this.GetProperty<int>("Enabled") != 0; }
            set
            {
                this["Enabled"] = value ? 1 : 0;
                if (this.Id != 0 && !value)
                    _inactivating = true;
            }
        }

        /// <inheritdoc />
        /// <remarks>Persisted as <see cref="RepositoryDataType.String"/>.</remarks>
        [RepositoryProperty("Domain", RepositoryDataType.String)]
        public string Domain
        {
            get { return this.GetProperty<string>("Domain"); }
            private set { this["Domain"] = value; }
        }

        /// <inheritdoc />
        /// <remarks>Persisted as <see cref="RepositoryDataType.String"/>.</remarks>
        [RepositoryProperty("Email")]
        public string Email
        {
            get { return this.GetProperty<string>("Email"); }
            set { this["Email"] = value; }
        }
        /// <inheritdoc />
        /// <remarks>Persisted as <see cref="RepositoryDataType.String"/>.</remarks>
        [RepositoryProperty("FullName")]
        public virtual string FullName
        {
            get { return this.GetProperty<string>("FullName"); }
            set { this["FullName"] = value; }
        }

        private const string OLDPASSWORDS = "OldPasswords";
        /// <summary>
        /// Gets or sets a serialized object that is the data store of the "old passwords" feature.
        /// Persisted as <see cref="RepositoryDataType.Text"/>.
        /// </summary>
        [RepositoryProperty(OLDPASSWORDS, RepositoryDataType.Text)]
        public string OldPasswords
        {
            get { return base.GetProperty<string>(OLDPASSWORDS); }
            set { base.SetProperty(OLDPASSWORDS, value); }
        }

        internal List<PasswordField.OldPasswordData> GetOldPasswords()
        {
            if (this.OldPasswords == null)
                return new List<PasswordField.OldPasswordData>();

            var serializer = new XmlSerializer(typeof(List<PasswordField.OldPasswordData>));
            using (var reader = new StringReader(this.OldPasswords))
            {
                var oldPasswords = serializer.Deserialize(reader) as List<PasswordField.OldPasswordData>;
                return oldPasswords;
            }
        }

        private void SetOldPasswords(List<PasswordField.OldPasswordData> oldPasswords)
        {
            if (oldPasswords == null)
                return;

            var serializer = new XmlSerializer(typeof(List<PasswordField.OldPasswordData>));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, oldPasswords);
                this.OldPasswords = writer.ToString();
            }
        }

        /// <inheritdoc />
        /// <remarks>This property is routed to the FullName property.</remarks>
        public override string DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(this.FullName) ? base.DisplayName : this.FullName;
            }
            set
            {
                this.FullName = value;
            }
        }

        /// <summary>
        /// Gets the URL of the avatar image.
        /// </summary>
        public virtual string AvatarUrl
        {
            get
            {
                // avatar is either image reference or image binary
                Image imageRef = null;
                var nodeList = this["ImageRef"] as IEnumerable<Node>;
                if (nodeList != null)
                    imageRef = nodeList.FirstOrDefault() as Image;

                var imageData = this["ImageData"] as BinaryData;


                // use imagefield static methods to get url for avatar
                var imageFieldData = new ImageField.ImageFieldData(null, imageRef, imageData);
                var imageRequestMode = ImageField.GetImageMode(imageFieldData);
                var imageUrl = ImageField.GetImageUrl(imageRequestMode, imageFieldData, this.Id, "ImageData");
                return imageUrl;
            }
        }
        /// <inheritdoc />
        /// <remarks>Persisted as <see cref="RepositoryDataType.String"/>.</remarks>
        [RepositoryProperty("PasswordHash")]
        public string PasswordHash
        {
            get { return this.GetProperty<string>("PasswordHash"); }
            set { this["PasswordHash"] = value; }
        }

        /// <inheritdoc />
        /// <remarks>The value depends on other property values by the following pattern:
        /// {Domain}\{LoginName | Name}</remarks>
        public string Username
        {
            get
            {
                // Domain hack - needed by the WebPI IIS7 Integrated mode
                var domain = PropertyTypes["Domain"] != null ? Domain : IdentityManagement.DefaultDomain;
                // install hack: username has to be accessible even during an upgrade, before we add the LoginName property
                var loginName = PropertyTypes[LOGINNAME] != null ? LoginName : Name;

                return string.Concat(domain, @"\", loginName);
            }
        }

        /// <inheritdoc />
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;

                // We have to fill the login name here and not in the Save method because
                // compulsory field validation would not make that possible.
                if (string.IsNullOrEmpty(LoginName))
                    LoginName = value;
            }
        }

        private const string LOGINNAME = "LoginName";
        /// <summary>
        /// Gets or sets the login name of this user.
        /// If this value is null, the value of the Name need to be used.
        /// Persisted as <see cref="RepositoryDataType.String"/>.
        /// </summary>
        [RepositoryProperty(LOGINNAME)]
        public virtual string LoginName
        {
            get { return this.GetProperty<string>(LOGINNAME); }
            set { this[LOGINNAME] = value; }
        }

        private const string PROFILE = "Profile";
        /// <summary>
        /// Gets or sets the <see cref="UserProfile"/> of this user, which is a workspace for storing
        /// personal documents, tasks or other content related to the user.
        /// Persisted as <see cref="RepositoryDataType.Reference"/>.
        /// </summary>
        [RepositoryProperty(PROFILE, RepositoryDataType.Reference)]
        public UserProfile Profile
        {
            get { return GetReference<UserProfile>(PROFILE); }
            set { SetReference(PROFILE, value); }
        }

        private const string PROFILEPATH = "ProfilePath";
        /// <summary>
        /// Gets the path of the user profile.
        /// </summary>
        public string ProfilePath => Profile == null ? GetProfilePath() : Profile.Path;

        private const string LANGUAGE = "Language";

        /// <summary>
        /// Gets or sets the code of the current user's preferred language.
        /// </summary>
        [RepositoryProperty(LANGUAGE)]
        public string Language
        {
            get { return this.GetProperty<string>(LANGUAGE); }
            set { this[LANGUAGE] = value; }
        }

        private const string FOLLOWEDWORKSPACES = "FollowedWorkspaces";
        /// <summary>
        /// Gets or sets the collection of <see cref="Node"/>s that represents the workspaces selected by this user to follow.
        /// </summary>
        [RepositoryProperty(FOLLOWEDWORKSPACES, RepositoryDataType.Reference)]
        public IEnumerable<Node> FollowedWorkspaces
        {
            get { return GetReferences(FOLLOWEDWORKSPACES); }
            set { SetReferences(FOLLOWEDWORKSPACES, value); }
        }


        // =================================================================================== Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public User(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public User(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class during the loading process.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected User(NodeToken token) : base(token) { }

        // =================================================================================== Methods

        /// <summary>
        /// Loads an existing <see cref="User"/> by login name provided in the following format: {Domain}\\{LoginName}
        /// </summary>
        /// <param name="domainUserName">Login name of the user.</param>
        public static User Load(string domainUserName)
        {
            int slashIndex = domainUserName.IndexOf('\\');
            string domain = "";
            string username;
            if (slashIndex != -1)
            {
                domain = domainUserName.Substring(0, slashIndex);
                username = domainUserName.Substring(slashIndex + 1);
            }
            else
            {
                username = domainUserName;
            }

            return Load(domain, username);
        }

        /// <summary>
        /// Loads an existing <see cref="User"/> by the given domain and name.
        /// </summary>
        /// <param name="domain">The name of the domain.</param>
        /// <param name="name">The Name or LoginName of the user.</param>
        /// <returns></returns>
        public static User Load(string domain, string name)
        {
            return Load(domain, name, ExecutionHint.None);
        }

        /// <summary>
        /// Loads an existing <see cref="User"/> by the given domain and name.
        /// </summary>
        /// <param name="domain">The name of the domain.</param>
        /// <param name="name">The Name or LoginName of the user.</param>
        /// <param name="hint">Internal or external search engine can be forced with this valule.</param>
        /// <returns></returns>
        public static User Load(string domain, string name, ExecutionHint hint)
        {
            domain = string.IsNullOrWhiteSpace(domain) ? IdentityManagement.DefaultDomain : domain;
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // look for the user ID in the cache by the doman-username key
            var ck = GetUserCacheKey(domain, name);
            var userIdobject = Cache.Get(ck);
            if (userIdobject != null)
            {
                var userId = Convert.ToInt32(userIdobject);
                var cachedUser = Node.Load<User>(userId);
                if (cachedUser != null)
                    return cachedUser;
            }

            var domainPath = string.Concat(RepositoryStructure.ImsFolderPath, RepositoryPath.PathSeparator, domain);
            var type = ActiveSchema.NodeTypes[typeof(User).Name];

            User user;
            bool forceCql;

            switch (hint)
            {
                case ExecutionHint.None: 
                    forceCql = SearchManager.ContentQueryIsAllowed; break;
                case ExecutionHint.ForceIndexedEngine: 
                    forceCql = true; break;
                case ExecutionHint.ForceRelationalEngine: 
                    forceCql = false; break;
                default:
                    throw new SnNotSupportedException("Unknown ExecutionHint: " + hint);
            }

            try
            {
                if (forceCql)
                {
                    var userResult = ContentQuery.Query(SafeQueries.UsersByLoginName, QuerySettings.AdminSettings, domainPath, name);
                    
                    // non-unique user, do not allow login
                    if (userResult.Count > 1)
                        return null;

                    user = userResult.Count == 0 ? null : userResult.Nodes.Cast<User>().FirstOrDefault();
                }
                else
                {
                    var queryProps = new List<QueryPropertyData>
                    {
                        new QueryPropertyData {PropertyName = LOGINNAME, QueryOperator = Operator.Equal, Value = name}
                    };

                    var userResult = NodeQuery.QueryNodesByTypeAndPathAndProperty(type, false, domainPath, false, queryProps);

                    // non-unique user, do not allow login
                    if (userResult.Count > 1)
                        return null;

                    user = userResult.Count == 0 ? null : userResult.Nodes.Cast<User>().FirstOrDefault();
                }
            }
            catch(Exception e)
            {
                SnLog.WriteException(e);
                return null;
            }

            if (user == null)
                return null;

            // insert id into cache
            if (Cache.Get(ck) == null)
                Cache.Insert(ck, user.Id, CacheDependencyFactory.CreateNodeDependency(user));

            return user;
        }
        private static string GetUserCacheKey(string domain, string name)
        {
            return string.Format("user-{0}-{1}", domain.Trim('\\').ToLower(), name.Trim('\\').ToLower());
        }

        /// <summary>
        /// Checks the given password and returns an instance of <see cref="PasswordCheckResult"/>.
        /// </summary>
        /// <param name="password">Password that will be checked.</param>
        /// <param name="oldPasswords">Old password hashes in a list of <see cref="PasswordField.OldPasswordData"/>.</param>
        /// <returns></returns>
        public virtual PasswordCheckResult CheckPassword(string password, List<PasswordField.OldPasswordData> oldPasswords)
        {
            return CheckPassword(this.GetContentType(), "Password", password, oldPasswords);
        }

        /// <summary>
        /// Checks the given password and returns an instance of <see cref="PasswordCheckResult"/>.
        /// </summary>
        /// <param name="contentType">The <see cref="ContentType"/> that has a password <see cref="FieldSetting"/>.</param>
        /// <param name="fieldName">The name of the field that's <see cref="FieldSetting"/> controls the inspection.</param>
        /// <param name="password">Password that will be checked.</param>
        /// <param name="oldPasswords">Old password hashes in a list of <see cref="PasswordField.OldPasswordData"/>.</param>
        /// <returns></returns>
        public PasswordCheckResult CheckPassword(ContentType contentType, string fieldName, string password, List<PasswordField.OldPasswordData> oldPasswords)
        {
            var pwFieldSetting = contentType.GetFieldSettingByName(fieldName) as PasswordFieldSetting;
            if (pwFieldSetting != null)
                throw new NotSupportedException(string.Format("Cannot check password if the field is not a PasswordField. ContentType: ", contentType, ", field: ", fieldName));
            return pwFieldSetting.CheckPassword(PasswordField.EncodePassword(password, this), oldPasswords);
        }

        /// <summary>
        /// Verifies whether the plain password matches the stored hash. Returns true if matches, otherwise false.
        /// </summary>
        /// <param name="passwordInClearText">The password entered by the user.</param>
        public bool CheckPasswordMatch(string passwordInClearText)
        {
            var match = false;
            try
            {
                // Check with the configured provider.
                match = PasswordHashProvider.CheckPassword(passwordInClearText, this.PasswordHash, this);
            }
            catch (SaltParseException)
            {
                // Keep 'match = false' and do not do other thing.
            }

            // If the migration is not enabled, shorting: return with the result.
            if (!Configuration.Security.EnablePasswordHashMigration)
                return match;

            // If password was matched the migration is not needed.
            if (match)
                return true;

            // Not match and migration is enabled.

            // Check with the outdated provider
            if (!PasswordHashProvider.CheckPasswordForMigration(passwordInClearText, this.PasswordHash, this))
                // If does not match, game over.
                return false;

            // Migration: generating a new hash with the configured provider and salt.
            this.PasswordHash = PasswordHashProvider.EncodePassword(passwordInClearText, this);

            using (new SystemAccount())
                Save(SavingMode.KeepVersion);

            return true;
        }

        /// <summary>
        /// Invalidates the pinned instances.
        /// </summary>
        [Obsolete("Do not use this method anymore.")]
        public static void Reset()
        {
            _somebody = null;
        }

        /// <summary>
        /// Technical method for loading or creating an administrator user.
        /// Do not use this method from your code directly.
        /// </summary>
        /// <param name="fullUserName">A username including the domain.</param>
        /// <returns>The loaded or newly created user.</returns>
        [Obsolete("Do not use this method anymore.", true)]
        public static User RegisterUser(string fullUserName)
        {
            throw new NotSupportedException();
        }

        // =================================================================================== Profile
        private UserProfileSettings _userProfileSettings; 
        private UserProfileSettings UserProfileSettings
        {
            get
            {
                if (_userProfileSettings == null)
                {
                    _userProfileSettings = Settings.GetValue<UserProfileSettings>("UserProfile", ContentType.Name, Settings.SETTINGSCONTAINERPATH);
                }
                return _userProfileSettings;
            }
        }

        private string GetProfileTemplateName()
        {
            return UserProfileSettings?.ProfileType ?? "UserProfile";
        }
        private string GetProfilesTargetPath()
        {
            return UserProfileSettings?.ProfilesTarget ?? RepositoryStructure.ImsFolderPath;
        }

        private string GetProfilesPath()
        {
            return RepositoryPath.Combine(GetProfilesTargetPath(), Profiles);
        }

        private string GetProfileParentPath()
        {
            return RepositoryPath.Combine(GetProfilesPath(), this.Domain ?? IdentityManagement.BuiltInDomainName);
        }

        private string GetProfileName()
        {
            return this.Name;
        }

        /// <summary>
        /// Returns the path of the profile workspace of this user.
        /// </summary>
        public string GetProfilePath()
        {
            return GetProfilePath(GetProfileName());
        }

        private string GetProfilePath(string profileName)
        {
            return RepositoryPath.Combine(GetProfileParentPath(), profileName);
        }

        /// <summary>
        /// Creates the profile structure of this user if the profile feature is enabled. Does nothing if the profile already exists.
        /// The structure tenplate can be specified as an optional parameter.
        /// The default template is the "UserProfile" <see cref="ContentTemplate"/>.
        /// </summary>
        /// <param name="template">Optional <see cref="Node"/> parameter of the profile template.</param>
        public void CreateProfile(Node template = null)
        {
            if (!IdentityManagement.UserProfilesEnabled)
                return;

            var upPath = ProfilePath;

            using (new SystemAccount())
            {
                if (Node.Exists(upPath))
                {
                    return;
                }

                var profileDomainPath = GetProfileParentPath();
                var profiles = Node.LoadNode(GetProfilesPath());
                if (profiles == null)
                {
                    var profilesTarget = Node.LoadNode(GetProfilesTargetPath());
                    var pc = Content.CreateNew(Profiles, profilesTarget, Profiles);
                    pc.Save();

                    var aclEditor = SecurityHandler.CreateAclEditor();
                    aclEditor.BreakInheritance(pc.Id, new[] {EntryType.Normal})
                        // ReSharper disable once CoVariantArrayConversion
                        .Allow(pc.Id, Identifiers.AdministratorsGroupId, false, PermissionType.PermissionTypes)
                        // ReSharper disable once CoVariantArrayConversion
                        .Allow(pc.Id, Identifiers.OwnersGroupId, false, PermissionType.PermissionTypes)
                        .Allow(pc.Id, Identifiers.EveryoneGroupId, true, PermissionType.Open)
                        .Apply();

                    profiles = pc.ContentHandler;
                }

                Content profile = null;
                var profileDomain = Node.LoadNode(profileDomainPath);
                Content domain;
                if (profileDomain == null)
                {
                    // create domain if not present
                    var domName = this.Domain ?? IdentityManagement.BuiltInDomainName;
                    domain = Content.CreateNew("ProfileDomain", profiles, domName);

                    // We set creator and modifier to Administrator here to avoid
                    // cases when a simple user becomes an author of a whole domain.
                    var admin = User.Administrator;
                    domain.ContentHandler.CreatedBy = admin;
                    domain.ContentHandler.VersionCreatedBy = admin;
                    domain.ContentHandler.Owner = admin;
                    domain.ContentHandler.ModifiedBy = admin;
                    domain.ContentHandler.VersionModifiedBy = admin;
                    domain.DisplayName = domName;

                    try
                    {
                        domain.Save();
                        profileDomain = domain.ContentHandler;
                    }
                    catch (NodeAlreadyExistsException)
                    {
                        // no problem, somebody else already created this domain in the meantime
                        profileDomain = Node.LoadNode(profileDomainPath);
                    }
                }

                template = template ?? ContentTemplate.GetNamedTemplate("UserProfile", GetProfileTemplateName());

                if (template == null)
                {
                    profile = Content.CreateNew("UserProfile", profileDomain, GetProfileName());
                }
                else
                {
                    var profNode = ContentTemplate.CreateFromTemplate(profileDomain, template, GetProfileName());
                    if (profNode != null)
                        profile = Content.Create(profNode);
                }

                if (profile != null)
                {
                    try
                    {
                        profile.ContentHandler.CreatedBy = this;
                        profile.ContentHandler.VersionCreatedBy = this;
                        profile.ContentHandler.Owner = this;
                        profile.DisplayName = this.Name;
                        profile.Save();

                        Profile = profile.ContentHandler as UserProfile;
                        Save(SavingMode.KeepVersion);
                    }
                    catch (Exception ex)
                    {
                        // error during user profile creation
                        SnLog.WriteException(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the profile feature is enabled and the user profile already exists.
        /// </summary>
        public bool IsProfileExist()
        {
            return IdentityManagement.UserProfilesEnabled && Node.Exists(GetProfilePath());
        }

        private void RenameProfile(string oldUsername)
        {
            if (!IdentityManagement.UserProfilesEnabled || string.IsNullOrEmpty(oldUsername))
                return;

            var newName = GetProfileName();
            if (oldUsername == newName)
                return;

            using (new SystemAccount())
            {
                var profile = Node.Load<UserProfile>(GetProfilePath(oldUsername));
                if (profile == null)
                    return;

                profile.Name = newName;
                profile.Save(SavingMode.KeepVersion);
            }
        }

        // =================================================================================== IUser Members

        /// <inheritdoc />
        public bool IsInGroup(IGroup group)
        {
            return SecurityHandler.IsInGroup(this.Id, group.Id);
        }
        /// <inheritdoc />
        public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit)
        {
            return SecurityHandler.IsInGroup(this.Id, orgUnit.Id);
        }
        /// <inheritdoc />
        public bool IsInContainer(ISecurityContainer container)
        {
            return SecurityHandler.IsInGroup(this.Id, container.Id);
        }

        private const string LASTLOGGEDOUT = "LastLoggedOut";
        /// <inheritdoc />
        [RepositoryProperty("LastLoggedOut", RepositoryDataType.DateTime)]
        public DateTime LastLoggedOut
        {
            get => GetProperty<DateTime>("LastLoggedOut");
            set => this["LastLoggedOut"] = value;
        }

        /// <summary>
        /// This method is obsolete. Use IsInGroup() instead.
        /// </summary>
        [Obsolete("Use IsInGroup instead.", false)]
        public bool IsInRole(int securityGroupId)
        {
            return IsInGroup(securityGroupId);
        }
        /// <inheritdoc />
        public bool IsInGroup(int securityGroupId)
        {
            return SecurityHandler.IsInGroup(this.Id, securityGroupId);
        }

        /// <inheritdoc />
        public string Password
        {
            set { _password = value; }
        }

        private MembershipExtension _membershipExtension;
        internal const string MembershipExtensionCallingKey = "MembershipExtensionCall";

        /// <summary>
        /// Gets or sets the <see cref="Storage.Security.MembershipExtension"/> instance
        /// that can customize the membership of this user.
        /// </summary>
        public MembershipExtension MembershipExtension
        {
            get
            {
                if (_membershipExtension == null || _membershipExtension == MembershipExtension.Placeholder)
                {
                    var called = GetCachedData(MembershipExtensionCallingKey) != null;
                    if (called)
                    {
                        SnTrace.Security.Write("MembershipExtenderRecursionGuard: recursion skipped. Path: {0}", Path);
                        return MembershipExtension.Placeholder;
                    }

                    using (var op = SnTrace.Security.StartOperation("MembershipExtenderRecursionGuard activation. Path: {0}", Path))
                    {
                        SetCachedData(MembershipExtensionCallingKey, true);

                        // this method calls the setter of this property, filling the member variable
                        MembershipExtenderBase.Extend(this);

                        SetCachedData(MembershipExtensionCallingKey, null);
                        op.Successful = true;
                    }
                }

                return _membershipExtension;
            }
            set => _membershipExtension = value;
        }
        
        /// <summary>
        /// This method is obsolete. Use GetGroups() instead.
        /// </summary>
        [Obsolete("Use GetGroups() instead.", true)]
        public List<int> GetPrincipals()
        {
            return GetGroups();
        }
        /// <summary>
        /// This method is obsolete. Use GetGroups() instead.
        /// </summary>
        [Obsolete("Use GetGroups() instead.", false)]
        public List<int> GetRoles()
        {
            return GetGroups();
        }
        /// <summary>
        /// Gets the ids of all the groups that contain the current user as a member, even through other groups,
        /// plus Everyone (except in case of a visitor) and the optional dynamic groups provided by the membership extender.
        /// </summary>
        public List<int> GetGroups()
        {
            return SecurityHandler.GetGroups(this);
        }

        // =================================================================================== IIdentity Members

        string System.Security.Principal.IIdentity.AuthenticationType
        {
            get { return "Portal"; }
        }
        bool System.Security.Principal.IIdentity.IsAuthenticated => Id != Identifiers.VisitorUserId && Id != 0;

        string System.Security.Principal.IIdentity.Name
        {
            get
            {
                return Username;
            }
        }

        private void SaveCurrentPassword()
        {
            var oldPasswords = this.GetOldPasswords();
            if (oldPasswords != null && oldPasswords.Count > 0)
            {
                // set oldpasswords if last password does not equal to current password
                if (oldPasswords.OrderBy(k => k.ModificationDate).Last().Hash != this.PasswordHash)
                    oldPasswords.Add(new PasswordField.OldPasswordData { ModificationDate = DateTime.UtcNow, Hash = this.PasswordHash });
            }
            else
            {
                if (this.PasswordHash != null)
                {
                    oldPasswords = new List<PasswordField.OldPasswordData>();
                    oldPasswords.Add(new PasswordField.OldPasswordData { ModificationDate = DateTime.UtcNow, Hash = this.PasswordHash });
                }
            }

            var passwordHistoryFieldMaxLength = Configuration.Security.PasswordHistoryFieldMaxLength;
            while (passwordHistoryFieldMaxLength + 1 < oldPasswords.Count)
                oldPasswords.RemoveAt(0);

            this.SetOldPasswords(oldPasswords);

        }

        /// <inheritdoc />
        /// <remarks>Synchronizes the AD modifications via the current <see cref="DirectoryProvider"/>.</remarks>
        public override void Save(NodeSaveSettings settings)
        {
            if (_inactivating)
            {
                AccessTokenVault.DeleteTokensByUser(this.Id);
                _inactivating = false;
            }

            // Check uniqueness first
            if (Id == 0 || PropertyNamesForCheckUniqueness.Any(p => IsPropertyChanged(p)))
                CheckUniqueUser();

            if (_password != null)
            {
                this.PasswordHash = PasswordHashProvider.EncodePassword(_password, this);
                this.LastLoggedOut = DateTime.UtcNow;
            }

            Domain = GenerateDomain();

            var originalId = this.Id;

            // save current password to the list of old passwords
            this.SaveCurrentPassword();

            base.Save(settings);

            // AD Sync
            SynchUser(originalId);

            if (originalId == 0)
            {
                // Set the creator and owner for convenient self permission setting.
                // The creator and owner of the user will always be the user itself. This way
                // setting permissions for the Owners group on /Root/IMS will be adequate
                // for user permissions.
                // If you need to know who the original creator is, use the audit log.
                // This is happening inside an elevated block, because the creator user may not have
                // the TakeOwnership permission which is required for this operation.
                using (new SystemAccount())
                {
                    Retrier.Retry(3, 200, typeof(Exception), () =>
                    {
                        // need to clear this flag to avoid getting an 'Id <> 0' error during copying
                        this.CopyInProgress = false;
                        this.CreatedBy = this;
                        this.Owner = this;
                        this.VersionCreatedBy = this;
                        this.DisableObserver(TypeResolver.GetType(NodeObserverNames.NOTIFICATION, false));
                        this.DisableObserver(TypeResolver.GetType(NodeObserverNames.WORKFLOWNOTIFICATION, false));

                        base.Save(SavingMode.KeepVersion);
                    });
                }

                // create profile
                if (IdentityManagement.UserProfilesEnabled)
                    CreateProfile();
            }

        }

        private string GenerateDomain()
        {
            var cutImsPath = Path.Substring(RepositoryStructure.ImsFolderPath.Length + 1);
            return cutImsPath.Substring(0, cutImsPath.IndexOf('/'));
        }

        private void SynchUser(int originalId)
        {
            if (_syncObject)
            {
                var ADProvider = DirectoryProvider.Current;
                if (ADProvider != null)
                {
                    if (originalId == 0)
                        ADProvider.CreateNewADUser(this, _password);
                    else
                        ADProvider.UpdateADUser(this, this.Path, _password);
                }
            }
            // default: object should be synced. if it was not synced now (sync properties updated only) next time it should be.
            _syncObject = true;
        }

        private void CheckUniqueUser()
        {
            var path = Path;

            if (!path.StartsWith(string.Concat(RepositoryStructure.ImsFolderPath, RepositoryPath.PathSeparator)) || ParentPath == RepositoryStructure.ImsFolderPath)
            {
                throw new InvalidOperationException("Invalid path: user nodes can only be saved under a /Root/IMS/[DomainName] folder.");
            }

            var domainPath = path.Substring(0, RepositoryStructure.ImsFolderPath.Length + 1 + path.Substring(RepositoryStructure.ImsFolderPath.Length + 1).IndexOf('/') + 1);

            // We validate here the uniqueness of the user. The constraint is the user name itself and that in Active Directory
            // there must not exist two users and/or groups with the same name under a domain. Organizational units may have
            // the same name as a user.
            // In Sense/Net not only the Name must be unique under a domain but the LoginName too, because that is what users
            // enter when they log in to the system. The current rule is the most restrictive possible: no group or user
            // may exist with the same name OR login name.

            List<int> identifiers;

            if (SearchManager.ContentQueryIsAllowed)
            {
                // We need to look for other users in elevated mode, because the current 
                // user may not have enough permissions for the whole user tree.
                using (new SystemAccount())
                {
                    var queryResult = ContentQuery.Query(SafeQueries.UserOrGroupByLoginName,
                        new QuerySettings {EnableAutofilters = FilterStatus.Disabled, Top = 2},
                        domainPath.TrimEnd('/'), 
                        Name,
                        LoginName ?? Name);

                    identifiers = queryResult.Identifiers.ToList();
                }
            }
            else
            {
                identifiers = new List<int>();

                var userType = ActiveSchema.NodeTypes["User"];
                var groupType = ActiveSchema.NodeTypes["Group"];

                // For backward compatibility reasons we have to execute up to 4 different 
                // SQL queries to make sure that the user is unique under a domain.

                // query users and groups by their names (for the current Name)
                var queryResult = NodeQuery.QueryNodesByTypeAndPathAndName(
                    new List<NodeType> { userType, groupType }, false,
                    domainPath, false, Name);
                identifiers.AddRange(queryResult.Identifiers);

                // query users by their LoginName (for the current Name)
                queryResult = NodeQuery.QueryNodesByTypeAndPathAndProperty(userType, false, domainPath, false, new List<QueryPropertyData>
                {
                    new QueryPropertyData {PropertyName = LOGINNAME, QueryOperator = Operator.Equal, Value = Name}
                });
                identifiers.AddRange(queryResult.Identifiers);
                
                // execute queries with the LoginName value only if it is different from the Name
                if (string.CompareOrdinal(Name, LoginName) != 0)
                {
                    // query users and groups by their names (for the current LoginName)
                    queryResult = NodeQuery.QueryNodesByTypeAndPathAndName(
                        new List<NodeType> { userType, groupType }, false,
                        domainPath, false, LoginName);

                    identifiers.AddRange(queryResult.Identifiers);

                    // query users by their LoginName (for the current LoginName)
                    queryResult = NodeQuery.QueryNodesByTypeAndPathAndProperty(userType, false, domainPath, false,
                        new List<QueryPropertyData>
                        {
                            new QueryPropertyData { PropertyName = LOGINNAME, QueryOperator = Operator.Equal, Value = LoginName }
                        });

                    identifiers.AddRange(queryResult.Identifiers);
                }
            }

            var existingList = identifiers.Distinct().ToArray();

            if (existingList.Length > 1 || (existingList.Length == 1 && existingList.First() != this.Id))
                throw GetUniqueUserException(domainPath);
        }
        private Exception GetUniqueUserException(string domainPath)
        {
            var message = string.Format(SR.GetString(SR.Exceptions.User.Error_NonUnique),
                RepositoryPath.GetFileName(domainPath.TrimEnd(RepositoryPath.PathSeparator[0])),
                Name,
                LoginName);

            return new InvalidOperationException(message);
        }

        /// <inheritdoc />
        /// <remarks>Synchronizes the removed object via the current <see cref="DirectoryProvider"/>.</remarks>
        public override void ForceDelete()
        {
            base.ForceDelete();

            // AD Sync
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                ADProvider.DeleteADObject(this);
            }
        }

        /// <inheritdoc />
        /// <remarks>In this case returns false: users cannot be moved to the Trash.</remarks>
        public override bool IsTrashable
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc />
        /// <remarks>Synchronizes the updates via the current <see cref="DirectoryProvider"/>.</remarks>
        public override void MoveTo(Node target)
        {
            base.MoveTo(target);

            // AD Sync
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                ADProvider.UpdateADUser(this, RepositoryPath.Combine(target.Path, this.Name), _password);
            }
        }

        // ================================================================================================== SenseNet.Security.ISecurityUser

        /// <summary>
        /// Returns ids of the dynamic groups that were added by the current <see cref="MembershipExtension"/>.
        /// </summary>
        public virtual IEnumerable<int> GetDynamicGroups(int entityId)
        {
            if (this.MembershipExtension == null)
                return new int[0];
            return this.MembershipExtension.ExtensionIds;
        }

        // =================================================================================== Events

        /// <summary>
        /// Checks whether the Move operation is acceptable for the current <see cref="DirectoryProvider"/>.
        /// The operation will be cancelled if it is prohibited.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnMoving(object sender, CancellableNodeOperationEventArgs e)
        {
            // AD Sync check
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                var targetNodePath = RepositoryPath.Combine(e.TargetNode.Path, this.Name);
                var allowMove = ADProvider.AllowMoveADObject(this, targetNodePath);
                if (!allowMove)
                {
                    e.CancelMessage = "Moving of synced nodes is only allowed within AD server bounds!";
                    e.Cancel = true;
                }
            }

            base.OnMoving(sender, e);
        }

        /// <summary>
        /// After a modification renames the user's profile if the user's name has changed.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnModified(object sender, NodeEventArgs e)
        {
            base.OnModified(sender, e);

            var nameData = e.ChangedData.FirstOrDefault(cd => cd.Name == "Name");
            if (nameData != null)
                RenameProfile(nameData.Original.ToString());
        }

        /// <summary>
        /// After creation adds this user to the nearest parent <see cref="OrganizationalUnit"/> as a member.
        /// Do not use this method directly from your code.
        /// </summary>
        protected override void OnCreated(object sender, NodeEventArgs e)
        {
            base.OnCreated(sender, e);

            using (new SystemAccount())
            {
                var parent = GroupMembershipObserver.GetFirstOrgUnitParent(e.SourceNode);
                if (parent != null)
                    SecurityHandler.AddUsersToGroup(parent.Id, new[] { e.SourceNode.Id });
            }
        }

        // =================================================================================== IADSyncable Members

        /// <summary>
        /// Updates the last AD sync id of this object.
        /// </summary>
        /// <param name="guid">A nullable GUID as sync id.</param>
        public void UpdateLastSync(Guid? guid)
        {
            if (guid.HasValue)
                this["SyncGuid"] = ((Guid)guid).ToString();
            this["LastSync"] = DateTime.UtcNow;

            // update object without syncing to AD
            _syncObject = false;

            this.Save();
        }

        // =================================================================================== Generic Property handlers

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "Enabled":
                    return this.Enabled;
                case "Email":
                    return this.Email;
                case "FullName":
                    return this.FullName;
                case "PasswordHash":
                    return this.PasswordHash;
                case "Domain":
                    return this.Domain;
                case OLDPASSWORDS:
                    return this.OldPasswords;
                case LANGUAGE:
                    return this.Language;
                case FOLLOWEDWORKSPACES:
                    return this.FollowedWorkspaces;
                case LOGINNAME:
                    return this.LoginName;
                case PROFILEPATH:
                    return this.ProfilePath;
                case LASTLOGGEDOUT:
                    return this.LastLoggedOut;
                default:
                    return base.GetProperty(name);
            }
        }
        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "Enabled":
                    this.Enabled = (bool)value;
                    break;
                case "Email":
                    this.Email = (string)value;
                    break;
                case "FullName":
                    this.FullName = (string)value;
                    break;
                case "PasswordHash":
                    this.PasswordHash = (string)value;
                    break;
                case "Domain":
                    this.Domain = (string)value;
                    break;
                case OLDPASSWORDS:
                    this.OldPasswords = (string)value;
                    break;
                case LANGUAGE:
                    this.Language = (string)value;
                    break;
                case FOLLOWEDWORKSPACES:
                    this.FollowedWorkspaces = (IEnumerable<Node>)value;
                    break;
                case LOGINNAME:
                    this.LoginName = (string)value;
                    break;
                case PROFILEPATH:
                    // this is a readonly property
                    break;
                case LASTLOGGEDOUT:
                    this.LastLoggedOut = (DateTime)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}

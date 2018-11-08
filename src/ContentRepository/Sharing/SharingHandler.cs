using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;
using Retrier = SenseNet.Tools.Retrier;

namespace SenseNet.ContentRepository.Sharing
{
    internal class SharingQueries : ISafeQueryHolder
    {
        /// <summary>Returns the following query: +TypeIs:User +Email:@0</summary>
        public static string UsersByEmail => "+TypeIs:User +Email:@0";
        /// <summary>Returns the following query: +SharedWith:@0</summary>
        public static string ContentBySharedWith => "+SharedWith:@0";
        /// <summary>Returns the following query: +SharedWith:@0 +SharedWith:0 +SharingMode:Private</summary>
        public static string PrivatelySharedWithNoIdentityByEmail => "+SharedWith:@0 +SharedWith:0 +SharingMode:Private";
        /// <summary>Returns the following query: +InTree:@0 +SharingMode:Public</summary>
        public static string PubliclySharedInTree => "+InTree:@0 +SharingMode:Public";
        /// <summary>Returns the following query: +TypeIs:SharingGroup +SharedContent:@0</summary>
        public static string SharingGroupsBySharedContent => "+TypeIs:SharingGroup +SharedContent:@0";
    }

    internal static class Constants
    {
        public const string SharingSessionKey = "SharingIdentity";
        public const string SharingGroupTypeName = "SharingGroup";
        public const string SharedWithFieldName = "SharedWith";
        public const string SharingModeFieldName = "SharingMode";
        public const string SharingIdsFieldName = "SharingIds";
        public const string SharedContentFieldName = "SharedContent";
        public const string SharingLevelValueFieldName = "SharingLevelValue";
        public const string SharingUrlParameterName = "share";
        public const string SharingSettingsName = "Sharing";
    }

    /// <summary>
    /// Central API entry point for managing content sharing.
    /// </summary>
    public class SharingHandler
    {
        private const string SharingItemsCacheKey = "SharingItems";

        // internal! getonly!
        //UNDONE: individual items should be immutable too
        private readonly object _itemsSync = new object();
        private List<SharingData> _items;

        /// <summary>
        /// Internal readonly list of all sharing records on a content.
        /// </summary>
        internal IEnumerable<SharingData> Items
        {
            get
            {
                //UNDONE: <? only for tests
                if (_items == null)
                {
                    lock (_itemsSync)
                    {
                        if (_items == null)
                        {
                            var src = _owner.SharingData;
                            if (string.IsNullOrEmpty(src))
                                _items = new List<SharingData>();

                            _items = Deserialize(src);

                            _owner.SetCachedData(SharingItemsCacheKey, _items);
                        }
                    }
                }
                return _items;
            }
        }

        /// <summary>
        /// Resets the pinned item list of sharing records.
        /// </summary>
        internal void ItemsChanged()
        {
            _items = null;
        }

        private const string SharingNotificationFormatterKey = "SharingNotificationFormatter";
        private static readonly object FormatterSync = new object();

        /// <summary>
        /// Gets or sets the provider responsible for formatting sharing notification
        /// email subject and body. Developers may customize the values and variables
        /// available in these texts.
        /// </summary>
        internal static ISharingNotificationFormatter NotificationFormatter
        {
            get
            {
                ISharingNotificationFormatter formatter;

                // ReSharper disable once InconsistentlySynchronizedField
                if ((formatter = Providers.Instance.GetProvider<ISharingNotificationFormatter>(
                        SharingNotificationFormatterKey)) != null)
                    return formatter;

                lock (FormatterSync)
                {
                    if ((formatter = Providers.Instance.GetProvider<ISharingNotificationFormatter>(
                            SharingNotificationFormatterKey)) != null)
                        return formatter;

                    // default implementation
                    formatter = new DefaultSharingNotificationFormatter();
                    Providers.Instance.SetProvider(SharingNotificationFormatterKey, formatter);
                }

                return formatter;
            }
            set => Providers.Instance.SetProvider(SharingNotificationFormatterKey, value);
        }

        private readonly GenericContent _owner;
        internal SharingHandler(GenericContent owner)
        {
            _owner = owner;

            // load deserialized item list from cached node data
            _items = _owner.GetCachedData(SharingItemsCacheKey) as List<SharingData>;
        }

        /* ================================================================================== Serialization */

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.Indented
        };
        internal static string Serialize(IEnumerable<SharingData> items)
        {
            var result = JsonConvert.SerializeObject(items, SerializerSettings);
            return result;
        }

        internal static List<SharingData> Deserialize(string source)
        {
            if(string.IsNullOrEmpty(source))
                return new List<SharingData>();

            return (List<SharingData>) JsonConvert.DeserializeObject(source, typeof(List<SharingData>),
                SerializerSettings);
        }

        /* ================================================================================== Public API */

        /// <summary>
        /// Readonly list of all sharing records on a content.
        /// </summary>
        public IEnumerable<SharingData> GetSharingItems()
        {
            return new ReadOnlyCollection<SharingData>(Items.ToList());
        }

        /// <summary>
        /// Share a content with an internal or external identity.
        /// </summary>
        /// <param name="token">Represents an identity. It can be an email address, username, user or group id.</param>
        /// <param name="level">Level of sharing.</param>
        /// <param name="mode">Sharing mode. Publicly shared content will be available for everyone.
        /// Authenticated mode means the content will be accessible by all logged in users in the system.
        /// Private sharing gives access only to the user defined in the token.</param>
        /// <param name="sendNotification">Whether to send a notification email to target user(s).</param>
        /// <returns>The newly created sharing record.</returns>
        public SharingData Share(string token, SharingLevel level, SharingMode mode, bool sendNotification = true)
        {
            AssertSharingPermissions();

            var sharingData = new SharingData
            {
                Token = token,
                Level = level,
                Mode = mode,
                CreatorId = (AccessProvider.Current.GetOriginalUser() as User)?.Id ?? 0,
                ShareDate = DateTime.UtcNow
            };

            // Get the identity using the id of the newly created sharing data object.
            // The id will be registered on the sharing group if the record is public.
            sharingData.Identity = GetSharingIdentity(sharingData.Id, token, level, mode);

            // make sure the list is loaded
            var _ = Items;

            _items.Add(sharingData);

            UpdateOwnerNode();
            SetPermissions(sharingData);

            if (sendNotification)
                NotifyTarget(sharingData);

            return sharingData;
        }
        /// <summary>
        /// Removes sharing from a content.
        /// </summary>
        /// <param name="ids">Identifiers of one or more sharing records.</param>
        /// <returns>True if an existing sharing record has been successfully deleted.</returns>
        public bool RemoveSharing(params string[] ids)
        {
            AssertSharingPermissions();

            // make sure te list is loaded
            var _ = Items;

            var sharingsToDelete = _items?.Where(sd => ids.Contains(sd.Id)).ToArray();
            if (sharingsToDelete == null)
                return false;

            var removedCount = _items.RemoveAll(sharingsToDelete.Contains);
            if (removedCount == 0)
                return false;

            UpdateOwnerNode();

            var identities = sharingsToDelete
                .Where(sd => sd.Identity > 0)
                .Select(sd => sd.Identity).Distinct();

            foreach (var identityId in identities)
            {
                // collect remaining sharing entries for this identity
                var remainData = _items.Where(x => x.Identity == identityId).ToArray();
                UpdatePermissions(identityId, remainData);
            }

            // in case of public sharing: delete the sharing group too
            if (sharingsToDelete.Any(sd => sd.Mode == SharingMode.Public))
            {
                using (new SystemAccount())
                {
                    foreach (var sharingData in sharingsToDelete.Where(sd => sd.Mode == SharingMode.Public))
                    {
                        var sharingGroup = Node.LoadNode(sharingData.Identity) as Group;
                        if (sharingGroup?.NodeType.IsInstaceOfOrDerivedFrom(Constants.SharingGroupTypeName) ?? false)
                            sharingGroup.ForceDelete();
                    }
                }
            }

            return true;
        }

        /* ================================================================================== Helper methods */

        private void SetPermissions(SharingData sharingData)
        {
            if (sharingData.Identity <= 0)
                return;

            var mask = GetEffectiveBitmask(sharingData.Level);
            SnSecurityContext.Create().CreateAclEditor(EntryType.Sharing)
                .Set(_owner.Id, sharingData.Identity, false, mask, 0ul)
                .Apply();
        }

        private void UpdatePermissions(int identityId, SharingData[] remainData)
        {
            UpdatePermissions(_owner.Id, identityId, remainData);
        }
        private static void UpdatePermissions(int nodeId, int identityId, SharingData[] remainData)
        {
            if (identityId <= 0)
                return;

            var mask = remainData.Aggregate(0ul, (current, item) => current | GetEffectiveBitmask(item.Level));

            SnSecurityContext.Create().CreateAclEditor(EntryType.Sharing)
                .Reset(nodeId, identityId, false, ulong.MaxValue, 0ul)
                .Set(nodeId, identityId, false, mask, 0ul)
                .Apply();

        }
        private void AssertSharingPermissions()
        {
            _owner?.Security.Assert(PermissionType.SetPermissions);
        }

        private int GetSharingIdentity(string id, string token, SharingLevel level, SharingMode mode)
        {
            switch (mode)
            {
                case SharingMode.Public:
                    // Public sharing: create a local or global group for every
                    // content+level combination
                    var group = LoadOrCreateGroup(id, level);

                    return group.Id;
                case SharingMode.Authenticated:
                    return Group.Everyone.Id;
            }

            return GetSharingIdentityByToken(token);
        }
        private static int GetSharingIdentityByToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return 0;

            // Currently we can recognize the following tokens:
            // - email address
            // - user or group id
            // - domain\username

            if (int.TryParse(token, out var id))
            {
                var node = Node.LoadNode(id);
                if (node != null && (node is User || node is Group))
                    return node.Id;

                // not found or not accessible
                return 0;
            }
            
            // Search for email address in elevated mode, because in case of
            // an email token the caller does not have to know about the user.
            if (token.Contains("@"))
            {
                var userId = SystemAccount.Execute(() =>
                    ContentQuery.Query(SharingQueries.UsersByEmail, QuerySettings.AdminSettings, token).Identifiers
                        .FirstOrDefault());

                if (userId > 0)
                    return userId;
            }
            
            // try for a username
            var user = User.Load(token);

            return user?.Id ?? 0;
        }

        private Group LoadOrCreateGroup(string id, SharingLevel level)
        {
            var ws = _owner.Workspace;

            // Look for the sharing group among local groups in the workspace
            // or (if there is no workspace) in the global folder under 
            // a dedicated domain.
            var parent = ws == null 
                ? LoadOrCreateContainer(RepositoryStructure.ImsFolderPath, "Sharing", "Domain") 
                : LoadOrCreateContainer(ws.Path, Workspace.LocalGroupsFolderName, "SystemFolder");

            if (parent is Domain domain && !domain.IsAllowedChildType(Constants.SharingGroupTypeName))
            {
                domain.AllowChildType(Constants.SharingGroupTypeName, save:true);
            }

            var group = Content.All.DisableAutofilters().FirstOrDefault(c =>
                c.InTree(parent) &&
                c.TypeIs(Constants.SharingGroupTypeName) &&
                (Node)c[Constants.SharedContentFieldName] == _owner &&
                (string)c[Constants.SharingLevelValueFieldName] == level.ToString());

            if (group == null)
            {
                // Sharing groups have special names for a reason: we need to have one and 
                // only one group per content per sharing level, so we cannot have a random 
                // name because that would allow the creation of multiple sharing groups for 
                // the same purpose on multiple threads.
                var groupName = $"G{_owner.Id}-{level.ToString().ToLowerInvariant()}";

                // create a new sharing group
                Retrier.Retry(3, 300, () =>
                {
                    group = Content.CreateNew(Constants.SharingGroupTypeName, parent, groupName);
                    group[Constants.SharingIdsFieldName] = id?.Replace("-", string.Empty);
                    group[Constants.SharedContentFieldName] = _owner;
                    group[Constants.SharingLevelValueFieldName] = level.ToString();
                    group.Save();
                }, (i, e) =>
                {
                    switch (e)
                    {
                        case null:
                            return true;
                        case NodeAlreadyExistsException _:
                            // created on another thread: reload by name
                            group = Content.Load($"{parent.Path}/{groupName}");
                            return true;
                    }

                    return false;
                });
            }
            else
            {
                // set sharing id on the group if it is not there yet
                group[Constants.SharingIdsFieldName] = AddSharingId((string)group[Constants.SharingIdsFieldName], id);
                group.SaveSameVersion();
            }

            return group.ContentHandler as Group;
        }
        private static Node LoadOrCreateContainer(string parentPath, string name, string contentTypeName)
        {
            var containerPath = $"{parentPath}/{name}";
            var container = Node.LoadNode(containerPath);
            if (container == null)
            {
                Retrier.Retry(3, 300, () =>
                {
                    var content = Content.CreateNew(contentTypeName, Node.LoadNode(parentPath), name);
                    content.Save();
                    container = content.ContentHandler;
                }, (i, ex) =>
                {
                    switch (ex)
                    {
                        case null:
                            return true;
                        case NodeAlreadyExistsException _:
                            // reload by name
                            container = Node.LoadNode(containerPath);
                            return true;
                    }

                    return false;
                });
            }

            return container;
        }
        
        private void UpdateOwnerNode()
        {
            // do not reset the item list because we already have it up-todate
            _owner.SetSharingData(Serialize(_items), false);
            _owner.Save(SavingMode.KeepVersion);

            _owner.SetCachedData(SharingItemsCacheKey, _items);
        }

        /* ================================================================================== Permissions */

        private static readonly Lazy<Dictionary<SharingLevel, ulong>> EffectiveBitmasks =
            new Lazy<Dictionary<SharingLevel, ulong>>(() =>
            {
                return Enum.GetValues(typeof(SharingLevel))
                    .Cast<SharingLevel>()
                    .ToDictionary(x => x, CalculateEffectiveBitmask);
            });

        internal static ulong GetEffectiveBitmask(SharingLevel level)
        {
            return EffectiveBitmasks.Value[level];
        }
        private static ulong CalculateEffectiveBitmask(SharingLevel level)
        {
            PermissionTypeBase[] permissions;
            switch (level)
            {
                case SharingLevel.Open:
                    permissions = new[] {PermissionType.Open};
                    break;
                case SharingLevel.Edit:
                    permissions = new[] { PermissionType.Save };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }

            var allPerms = permissions.ToList();
            var index = -1;
            while (++index < allPerms.Count)
                allPerms.AddRange(allPerms[index].Allows ?? Enumerable.Empty<PermissionTypeBase>());

            var bits = 0ul;
            foreach (var perm in allPerms)
                bits |= perm.Mask;

            return bits;
        }

        internal List<AceInfo> GetExplicitEntries()
        {
            return GetExplicitEntries(_owner.Id);
        }
        internal static List<AceInfo> GetExplicitEntries(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            SecurityHandler.SecurityContext.AssertPermission(contentId, PermissionType.SeePermissions);
            return GetExplicitEntriesAsSystemUser(contentId, relatedIdentities);
        }
        internal static List<AceInfo> GetExplicitEntriesAsSystemUser(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            return SecurityHandler.SecurityContext.GetExplicitEntries(contentId, relatedIdentities, EntryType.Sharing);
        }

        internal List<AceInfo> GetEffectiveEntries()
        {
            return GetEffectiveEntries(_owner.Id);
        }
        internal static List<AceInfo> GetEffectiveEntries(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            SecurityHandler.SecurityContext.AssertPermission(contentId, PermissionType.SeePermissions);
            return GetEffectiveEntriesAsSystemUser(contentId, relatedIdentities);
        }
        internal static List<AceInfo> GetEffectiveEntriesAsSystemUser(int contentId, IEnumerable<int> relatedIdentities = null)
        {
            return SecurityHandler.SecurityContext.GetEffectiveEntries(contentId, relatedIdentities, EntryType.Sharing);
        }

        /* ================================================================================== Notifications */

        private void NotifyTarget(SharingData sharingData)
        {
            if (_owner == null || string.IsNullOrEmpty(sharingData?.Token))
                return;
            if (!Settings.GetValue(Constants.SharingSettingsName, "NotificationEnabled", _owner.Path, false))
                return;

            //TODO: prepare for recognizing other types of identities: user or group

            // Not an email: currently do nothing.
            if (!sharingData.Token.Contains("@"))
                return;

            var siteUrl = HttpContext.Current?.Request.Url
                          .GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) ??
                          "http://example.com";

            System.Threading.Tasks.Task.Run(() => NotifyTarget(sharingData.Token, sharingData, siteUrl));
        }
        private void NotifyTarget(string email, SharingData sharingData, string siteUrl)
        {
            // Settings makes possible to customize notification values based on subtree
            // (e.g. different letters under different sites or workspaces).
            var senderAddress = Settings.GetValue(Constants.SharingSettingsName, "NotificationSender", _owner.Path, "info@example.com");
            var mailSubjectKey = Settings.GetValue(Constants.SharingSettingsName, "NotificationMailSubjectKey", _owner.Path, "NotificationMailSubject");
            var mailBodyKey = Settings.GetValue(Constants.SharingSettingsName, "NotificationMailBodyKey", _owner.Path, "NotificationMailBody");

            var mailSubject = SR.GetString(Constants.SharingSettingsName, mailSubjectKey);
            var mailBody = SR.GetString(Constants.SharingSettingsName, mailBodyKey);

            var formatter = NotificationFormatter;
            if (formatter != null)
            {
                mailSubject = formatter.FormatSubject(_owner, sharingData, mailSubject);
                mailBody = formatter.FormatBody(_owner, sharingData, siteUrl, mailBody);
            }

            var mailMessage = new MailMessage(senderAddress, email)
            {
                Subject = mailSubject,
                IsBodyHtml = true,
                Body = mailBody
            };

            try
            {
                using (var smtpClient = new SmtpClient())
                    smtpClient.Send(mailMessage);
            }
            catch (Exception ex) // logged
            {
                SnLog.WriteException(ex);
            }
        }

        /* ================================================================================== Event handlers */

        //UNDONE: execute event handler code in a Task asynchronously.

        internal static void OnContentDeleted(Node node)
        {
            if (node == null)
                return;

            using (new SystemAccount())
            {
                switch (node)
                {
                    case User _:
                    case Group _:
                        RemoveIdentities(new[] { node });
                        break;
                    default:
                        var identities = Content.All.DisableAutofilters().Where(c =>
                            c.InTree(node.Path) &&
                            (c.TypeIs("User") || c.TypeIs("Group"))).AsEnumerable().Select(c => c.ContentHandler);

                        RemoveIdentities(identities);
                        break;
                }
            }
        }
        internal static void OnUserCreated(User user)
        {
            if (string.IsNullOrEmpty(user?.Email))
                return;

            using (new SystemAccount())
            {
                UpdateIdentity(user);
            }
        }
        internal static void OnUserChanged(User user, string oldEmail)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                // Email address was removed from the user's profile.
                // Do nothing: shared content will still be accessible
                // to the user, because their identity is still valid.
                // The email will remain on the record as a historical info.
            }

            // The user got an email address: connect to sharing records created for this email.
            // The previous email will remain on the record as a historical info.
            using (new SystemAccount())
            {
                UpdateIdentity(user);
            }
        }

        private static void UpdateIdentity(User user)
        {
            if (string.IsNullOrEmpty(user.Email))
                return;

            // A new user has been created or an existing user got an email address: 
            // Iterate through existing sharing records for this email 
            // and add user id and set permissions for this user on the content.

            // Collect all content that has been shared with the email of this user.
            var results = ContentQuery.Query(SharingQueries.PrivatelySharedWithNoIdentityByEmail,
                QuerySettings.AdminSettings, user.Email);

            ProcessContentWithRetry(results.Nodes, gc =>
            {
                var content = Node.Load<GenericContent>(gc.Id);
                var changed = false;

                var newItems = content.Sharing.Items.Select(sd =>
                {
                    if (sd.Token != user.Email || sd.Identity != 0 || sd.Mode != SharingMode.Private)
                        return sd;
                    
                    sd.Identity = user.Id;
                    changed = true;

                    return sd;
                });

                content.SharingData = Serialize(newItems);
                content.Save(SavingMode.KeepVersion);

                // set permissions for the user
                if (changed)
                {
                    UpdatePermissions(content.Id, user.Id,
                        content.Sharing.Items.Where(sd => sd.Identity == user.Id).ToArray());
                }
            });
        }
        private static void RemoveIdentities(IEnumerable<Node> identities)
        {
            // Identities can be users or groups. This method removes all sharing records
            // that belong to any of the user/group ids or user emails.

            var ids = new List<object>();
            var emails = new List<object>();

            // collect all user/group ids and emails related to these identities
            foreach (var identity in identities)
            {
                ids.Add(identity.Id);
                if (identity is User user && !string.IsNullOrEmpty(user.Email))
                    emails.Add(user.Email);
            }

            // collect all content that has been shared with these identities
            var results = ContentQuery.Query(SharingQueries.ContentBySharedWith,
                QuerySettings.AdminSettings, ids.Concat(emails).ToArray());

            ProcessContentWithRetry(results.Nodes,
                gc =>
                {
                    // collect all sharing records that belong to the provided identities
                    return gc.Sharing.Items.Where(sd => ids.Contains(sd.Identity) || emails.Contains(sd.Token));
                },
                (gc, recordsToRemove) =>
                {
                    // reload node and remove sharing
                    var content = Node.Load<GenericContent>(gc.Id);
                    content.Sharing.RemoveSharing(recordsToRemove.Select(sd => sd.Id).ToArray());
                });
        }

        internal static int[] GetSharingGroupIds(Node sharedContent)
        {
            if (sharedContent == null)
                return new int[0];

            // collect all content ids that were shared publicly in this subtree
            var publicSharedIds = ContentQuery.Query(SharingQueries.PubliclySharedInTree, 
                    QuerySettings.AdminSettings, sharedContent.Path).Identifiers.ToArray();

            if (!publicSharedIds.Any())
                return new int[0];

            // collect all group ids that will become orphanes
            return ContentQuery.Query(SharingQueries.SharingGroupsBySharedContent,
                QuerySettings.AdminSettings, publicSharedIds).Identifiers.ToArray();
        }

        /* ================================================================================== Helper methods */

        private static void ProcessContentWithRetry(IEnumerable<Node> nodes, Action<GenericContent> action)
        {
            Parallel.ForEach(nodes.Where(n => n is GenericContent).Cast<GenericContent>(),
                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                gc =>
                {
                    Retrier.Retry(3, 300, () => { action(gc); }, HandleRetryCondition);
                });
        }
        private static void ProcessContentWithRetry(IEnumerable<Node> nodes, 
            Func<GenericContent, IEnumerable<SharingData>> collectSharingData, 
            Action<GenericContent, IEnumerable<SharingData>> action)
        {
            Parallel.ForEach(nodes.Where(n => n is GenericContent).Cast<GenericContent>(),
                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                gc =>
                {
                    var sharingDataList = collectSharingData(gc);

                    Retrier.Retry(3, 300, () => { action(gc, sharingDataList); }, HandleRetryCondition);
                });
        }
        private static bool HandleRetryCondition(int iteration, Exception ex)
        {
            if (ex == null)
                return true;

            // we should retry
            if (ex is NodeIsOutOfDateException)
                return false;

            // log and leave
            SnLog.WriteException(ex);
            return true;
        }

        private static string AddSharingId(string original, string id)
        {
            if (string.IsNullOrEmpty(id))
                return original;

            var newId = id.Replace("-", string.Empty);
            var sharingIds = original?.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList() ??
                              new List<string>();

            if (!sharingIds.Contains(newId))
                sharingIds.Add(newId);

            return string.Join(" ", sharingIds);
        }

        public static Content GetSharingGroupFromUrl(NameValueCollection parameters)
        {
            var sharingGuid = GetSharingGuidFromUrl(parameters);
            if (string.IsNullOrEmpty(sharingGuid))
                return null;

            return GetSharingGroupBySharingId(sharingGuid);
        }
        public static string GetSharingGuidFromUrl(NameValueCollection parameters)
        {
            return parameters?[Constants.SharingUrlParameterName];
        }
        internal static SharingData GetSharingDataBySharingId(string sharingId)
        {
            var sharingGroup = GetSharingGroupBySharingId(sharingId);
            if (!(sharingGroup?[Constants.SharedContentFieldName] is GenericContent target))
                return null;

            return target.Sharing.Items.FirstOrDefault(sd => sd.Id == sharingId);
        }
        internal static Content GetSharingGroupBySharingId(string sharingGuid)
        {
            if (!string.IsNullOrEmpty(sharingGuid))
            {
                return Content.All.DisableAutofilters()
                    .Where(c => c.TypeIs(Constants.SharingGroupTypeName))
                    .FirstOrDefault(c =>
                        (string) c[Constants.SharingIdsFieldName] == sharingGuid.Replace("-", string.Empty));
            }

            return null;
        }
    }
}
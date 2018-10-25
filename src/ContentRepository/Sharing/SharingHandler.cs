using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
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
        /// <summary>Returns the following query: +SharedWith:@0 +SharedWith:0</summary>
        public static string ContentBySharedEmail => "+SharedWith:@0 +SharedWith:0";
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

            var identity = mode == SharingMode.Authenticated
                ? Group.Everyone.Id
                : GetSharingIdentityByToken(token);

            var sharingData = new SharingData
            {
                Token = token,
                Identity = identity,
                Level = level,
                Mode = mode,
                CreatorId = (AccessProvider.Current.GetOriginalUser() as User)?.Id ?? 0,
                ShareDate = DateTime.UtcNow
            };

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
            if (identityId <= 0)
                return;

            var mask = remainData.Aggregate(0ul, (current, item) => current | GetEffectiveBitmask(item.Level));

            SnSecurityContext.Create().CreateAclEditor(EntryType.Sharing)
                .Reset(_owner.Id, identityId, false, ulong.MaxValue, 0ul)
                .Set(_owner.Id, identityId, false, mask, 0ul)
                .Apply();

        }
        private void AssertSharingPermissions()
        {
            _owner?.Security.Assert(PermissionType.SetPermissions);
        }

        private int GetSharingIdentityByToken(string token)
        {
            //UNDONE: get sharing identity: email, username, groupname, special tokens etc.

            // returns the id of user by email or 0.
            var userId = ContentQuery.Query(SharingQueries.UsersByEmail, QuerySettings.AdminSettings, token)
                .Identifiers.FirstOrDefault();
            return userId;
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

        private const string SharingSettingsName = "Sharing";

        private void NotifyTarget(SharingData sharingData)
        {
            if (_owner == null || string.IsNullOrEmpty(sharingData?.Token))
                return;
            if (!Settings.GetValue(SharingSettingsName, "NotificationEnabled", _owner.Path, false))
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
            var senderAddress = Settings.GetValue(SharingSettingsName, "NotificationSender", _owner.Path, "info@example.com");
            var mailSubjectKey = Settings.GetValue(SharingSettingsName, "NotificationMailSubjectKey", _owner.Path, "NotificationMailSubject");
            var mailBodyKey = Settings.GetValue(SharingSettingsName, "NotificationMailBodyKey", _owner.Path, "NotificationMailBody");

            var mailSubject = SR.GetString(SharingSettingsName, mailSubjectKey);
            var mailBody = SR.GetString(SharingSettingsName, mailBodyKey);

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
                            (c.TypeIs("User") || c.TypeIs("Group"))).Select(c => c.ContentHandler);

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

        private static void UpdateIdentity(User user)
        {
            // Collect all content that has been shared with the email of this user.
            var results = ContentQuery.Query(SharingQueries.ContentBySharedEmail,
                QuerySettings.AdminSettings, user.Email);

            Parallel.ForEach(results.Nodes.Where(n => n is GenericContent).Cast<GenericContent>(),
                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                gc =>
                {
                    // retry a few times to update sharing
                    Retrier.Retry(3, 300, () =>
                        {
                            var content = Node.Load<GenericContent>(gc.Id);

                            var newItems = content.Sharing.Items.Select(sd =>
                            {
                                if (sd.Token != user.Email || sd.Identity != 0)
                                    return sd;

                                sd.Identity = user.Id;
                                return sd;
                            });

                            //UNDONE: set permissions for the user
                            // Maybe use the built-in API.

                            content.SharingData = Serialize(newItems);
                            content.Save(SavingMode.KeepVersion);
                        },
                        (i, e) =>
                        {
                            if (e == null)
                                return true;

                            // we should retry
                            if (e is NodeIsOutOfDateException)
                                return false;

                            // log and leave
                            SnLog.WriteException(e);
                            return true;
                        });
                });
        }

        private static void RemoveIdentities(IEnumerable<Node> identities)
        {
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

            Parallel.ForEach(results.Nodes.Where(n => n is GenericContent).Cast<GenericContent>(),
                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                gc =>
                {
                    // collect all sharing records that belong to the provided identities
                    var recordsToRemove =
                        gc.Sharing.Items.Where(sd => ids.Contains(sd.Identity) || emails.Contains(sd.Token));

                    // retry a few times to remove sharing
                    Retrier.Retry(3, 300, () =>
                        {
                            var content = Node.Load<GenericContent>(gc.Id);
                            content.Sharing.RemoveSharing(recordsToRemove.Select(sd => sd.Id).ToArray());
                        },
                        (i, e) =>
                        {
                            if (e == null)
                                return true;

                            // we should retry
                            if (e is NodeIsOutOfDateException)
                                return false;

                            // log and leave
                            SnLog.WriteException(e);
                            return true;
                        });
                });
        }
    }
}

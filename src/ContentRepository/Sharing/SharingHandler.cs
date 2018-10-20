﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Web;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Sharing
{
    internal class SharingQueries : ISafeQueryHolder
    {
        /// <summary>Returns the following query: +TypeIs:User +Email:@0</summary>
        public static string UsersByEmail => "+TypeIs:User +Email:@0";
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
        /// <param name="id">Identifies a sharing record.</param>
        /// <returns>True if an existing sharing record has been successfully deleted.</returns>
        public bool RemoveSharing(string id)
        {
            AssertSharingPermissions();

            // make sure te list is loaded
            var _ = Items;

            var sharingToDelete = _items?.FirstOrDefault(sd => sd.Id == id);
            if (sharingToDelete == null)
                return false;

            _items.Remove(sharingToDelete);

            UpdateOwnerNode();

            var identityId = sharingToDelete.Identity;
            if (identityId > 0)
            {
                var remainData = _items.Where(x => x.Identity == sharingToDelete.Identity).ToArray();
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

            System.Threading.Tasks.Task.Run(() => NotifyTarget(sharingData.Token, sharingData.Id, siteUrl));
        }
        private void NotifyTarget(string email, string guid, string siteUrl)
        {
            var senderAddress = Settings.GetValue(SharingSettingsName, "NotificationSender", _owner.Path, "info@example.com");
            var mailSubjectKey = Settings.GetValue(SharingSettingsName, "NotificationMailSubjectKey", _owner.Path, "NotificationMailSubject");
            var mailBodyKey = Settings.GetValue(SharingSettingsName, "NotificationMailBodyKey", _owner.Path, "NotificationMailBody");

            //TODO: send a site-relative path
            // Alternative: send an absolute path, but when a request arrives
            // containing a share guid, redirect to the more compact and readable path.
            var url = $"{siteUrl?.TrimEnd('/')}{_owner.Path}?share={guid}";

            var mailSubject = SR.GetString(SharingSettingsName, mailSubjectKey);
            var mailBody = string.Format(SR.GetString(SharingSettingsName, mailBodyKey), url);

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
    }
}

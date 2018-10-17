using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging.Steps;
using SenseNet.Search;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Sharing
{
    internal class SharingQueries : ISafeQueryHolder
    {
        /// <summary>Returns the following query: +TypeIs:User +Email:@0</summary>
        public static string UsersByEmail => "+TypeIs:User +Email:@0";
    }

    public class SharingHandler
    {
        private const string SharingItemsCacheKey = "SharingItems";

        // internal! getonly!
        //UNDONE: individual items should be immutable too
        private readonly object _itemsSync = new object();
        private List<SharingData> _items;
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
            //TypeNameHandling = TypeNameHandling.None,
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

        public SharingData Share(string token, SharingLevel level, SharingMode mode)
        {
            //UNDONE: finalize sharing public API

            //UNDONE: check/assert permission

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

            // make sure te list is loaded
            var _ = Items;

            _items.Add(sharingData);

            UpdateOwnerNode();
            SetPermissions(sharingData);

            //UNDONE: send notification email to the target identity

            return sharingData;
        }

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
            throw new NotImplementedException(); //UNDONE: UpdatePermissions is not implemented.
        }

        private int GetSharingIdentityByToken(string token)
        {
            //UNDONE: get sharing identity: email, username, groupname, special tokens etc.

            // returns the id of user by email or 0.
            var userId = ContentQuery.Query(SharingQueries.UsersByEmail, QuerySettings.AdminSettings, token)
                .Identifiers.FirstOrDefault();
            return userId;
        }
        
        public bool RemoveSharing(string id)
        {
            // make sure te list is loaded
            var _ = Items;

            //UNDONE: check/assert permission
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
    }
}

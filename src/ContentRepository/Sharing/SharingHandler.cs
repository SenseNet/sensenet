using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Security;

namespace SenseNet.ContentRepository.Sharing
{
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

                            //UNDONE: should we cache sharing items here?
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

            //UNDONE: set sharing identity if found
            var sharingData = new SharingData
            {
                Token = token,
                Level = level,
                Mode = mode,
                CreatorId = (AccessProvider.Current.GetOriginalUser() as User)?.Id ?? 0,
                ShareDate = DateTime.UtcNow,
                //Identity = 
            };

            _items.Add(sharingData);

            UpdateOwnerNode();

            return sharingData;
        }

        public bool RemoveSharing(string id)
        {
            var sharingData = _items?.FirstOrDefault(sd => sd.Id == id);
            if (sharingData == null)
                return false;

            _items.Remove(sharingData);

            UpdateOwnerNode();

            return true;
        }

        private void UpdateOwnerNode()
        {
            //UNDONE: sharing items: should we set null or _items?
            // ...because the _items list may be cleared below!
            _owner.SetCachedData(SharingItemsCacheKey, null);

            //UNDONE: this property setter resets the _items list unnecessarily!
            _owner.SharingData = Serialize(_items);
            _owner.Save(SavingMode.KeepVersion);
        }

        /* ================================================================================== Permissions */

        private static readonly Lazy<Dictionary<SharingLevel, ulong>> _effectiveBitmasks =
            new Lazy<Dictionary<SharingLevel, ulong>>(() =>
            {
                return Enum.GetValues(typeof(SharingLevel))
                    .Cast<SharingLevel>()
                    .ToDictionary(x => x, CalculateEffectiveBitmask);
            });

        internal static ulong GetEffectiveBitmask(SharingLevel level)
        {
            return _effectiveBitmasks.Value[level];
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

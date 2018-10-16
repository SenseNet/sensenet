using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Sharing
{
    public class SharingHandler
    {
        // internal! getonly!
        //UNDONE: individual items should be immutable too
        private readonly object _itemsSync = new object();
        private IEnumerable<SharingData> _items;
        internal IEnumerable<SharingData> Items {
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
                                _items = new SharingData[0];
                            _items = Deserialize(src);
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

            //UNDONE: sharing deserialization
            var sharingData = _owner.SharingData;

            // store deserialized item list in cached node data?
            //_owner.SetCachedData() ?
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

        internal static IEnumerable<SharingData> Deserialize(string source)
        {
            if(string.IsNullOrEmpty(source))
                return new SharingData[0];
            var result = (IEnumerable<SharingData>)JsonConvert.DeserializeObject(source, 
                typeof(IEnumerable<SharingData>), SerializerSettings);
            return result;
        }

        /* ================================================================================== Public API */

        public void Share(string token, SharingLevel level, SharingMode mode)
        {
            //UNDONE: finalize sharing public API

            // 1. add sharing record to Items
            // 2. serialize items
            // 3. set sharingdata property and save the content

            //_owner.SetCachedData(); ?

            throw new NotImplementedException();
        }

        public void RemoveSharing(string email)
        {
            //UNDONE: finalize sharing public API
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Sharing
{
    public class SharingHandler
    {
        // internal! getonly!
        //UNDONE: individual items should be immutable too
        internal IEnumerable<SharingData> Items { get; }

        private readonly GenericContent _owner;
        internal SharingHandler(GenericContent owner)
        {
            _owner = owner;

            //UNDONE: sharing deserialization
            var sharingData = _owner.SharingData;

            // store deserialized item list in cached node data?
            //_owner.SetCachedData() ?
        }

        // ================================================================================== Public API 
        
        public void Share(string email)
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

using System;
using System.Globalization;

namespace SenseNet.Portal.Virtualization
{
    internal sealed class HashCodeCombiner
    {
        // Start with a seed
        private long _combinedHash = 5381;

        internal void AddLong(long l)
        {
            _combinedHash = ((_combinedHash << 5) + _combinedHash) ^ l;
        }

        internal string CombinedHashString
        {
            get
            {
                return _combinedHash.ToString("x", CultureInfo.InvariantCulture);
            }
        }


    }
}
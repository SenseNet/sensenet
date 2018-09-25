using System;
using System.Web;

namespace SenseNet.ContentRepository.Storage.Caching.Legacy
{
    public interface ICache : ISnCache
    {
        DateTime NoAbsoluteExpiration { get; }
        TimeSpan NoSlidingExpiration { get; }

        long EffectivePercentagePhysicalMemoryLimit { get; }
        long EffectivePrivateBytesLimit { get; }

        HttpContext CurrentHttpContext { get; set; }
    }
}

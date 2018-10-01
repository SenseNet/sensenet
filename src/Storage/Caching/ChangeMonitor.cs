using System;
using System.Globalization;
using System.Runtime.Caching;

namespace SenseNet.ContentRepository.Storage.Caching
{
    internal abstract class ChangeMonitorBase : ChangeMonitor
    {
        public override string UniqueId { get; } =
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    }
}

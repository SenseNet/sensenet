using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.ContentRepository.Storage.Caching.Builtin
{
    internal abstract class ChangeMonitorBase : ChangeMonitor
    {
        public override string UniqueId { get; } =
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Security.ADSync
{
    public interface IADSyncable
    {
        void UpdateLastSync(Guid? guid);
    }
}

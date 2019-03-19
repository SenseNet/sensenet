using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Storage
{
    internal class SharedLockCleanupTask : IMaintenanceTask
    {
        public double WaitingMinutes { get; internal set; } = 70.0d; // 1:10:00

        public void Execute()
        {
            SharedLock.Cleanup();
        }
    }
}

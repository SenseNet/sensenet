using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Storage.Security
{
    internal class AccessTokenCleanupTask : IMaintenanceTask
    {
        public double WaitingMinutes { get; internal set; } = 75.0d; // 1:15:00

        public void Execute()
        {
            AccessTokenVault.Cleanup();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class SystemAccount : IDisposable
    {
        public SystemAccount()
        {
            AccessProvider.ChangeToSystemAccount();
        }

        public void  Dispose()
        {
            AccessProvider.RestoreOriginalUser();
        }

        public static T Execute<T>(Func<T> function)
        {
            using (new SystemAccount())
            {
                return (T)function();
            }
        }
    }
}

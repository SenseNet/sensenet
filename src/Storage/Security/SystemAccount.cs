using System;
using System.Threading.Tasks;

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
                return function();
            }
        }

        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> function)
        {
            using (new SystemAccount())
            {
                return await function();
            }
        }
    }
}

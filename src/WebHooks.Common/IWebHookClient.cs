using System;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.WebHooks.Common
{
    public interface IWebHookClient
    {
        Task CallServiceAsync(string url, CancellationToken cancel = default);
    }
}

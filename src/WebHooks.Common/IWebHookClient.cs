using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.WebHooks
{
    public interface IWebHookClient
    {
        Task SendAsync(string url, string httpMethod = "POST", object postData = null,
            IDictionary<string, string> headers = null, CancellationToken cancel = default);
    }
}

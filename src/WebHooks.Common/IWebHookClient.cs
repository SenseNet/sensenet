using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.WebHooks.Common
{
    public interface IWebHookClient
    {
        Task SendAsync(string url, string httpMethod = "POST", object postData = null,
            IDictionary<string, string> headers = null, CancellationToken cancel = default);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.WebHooks
{
    /// <summary>
    /// Defines methods for sending webhook events.
    /// </summary>
    public interface IWebHookClient
    {
        //TODO: add optional parameter for content type
        Task SendAsync(string url, string eventName, int contentId, int subscriptionId, string httpMethod = "POST",
            object postData = null, IDictionary<string, string> headers = null, CancellationToken cancel = default);
    }
}

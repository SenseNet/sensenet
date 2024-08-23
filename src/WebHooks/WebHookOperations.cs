using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.WebHooks
{
    public static class WebHookOperations
    {
        /// <summary>
        /// Fires the target webhook on the provided content for testing purposes.
        /// </summary>
        /// <snCategory>WebHooks</snCategory>
        /// <param name="webHookContent"></param>
        /// <param name="context"></param>
        /// <param name="path">Target content path.</param>
        /// <param name="eventType">Event type to simulate. Can be one of the available events: All, Create, Delete, Modify, Approve,
        /// Reject, Draft, Pending, CheckOut, MoveToTrash, RestoreFromTrash</param>
        /// <returns>The webhook subscription that was fired.</returns>
        [ODataAction(Category = "WebHooks")]
        [ContentTypes("WebHookSubscription")]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<Content> FireWebHook(Content webHookContent, HttpContext context, string path, WebHookEventType eventType)
        {
            await FireWebHook(context, (WebHookSubscription) webHookContent.ContentHandler, Node.LoadNode(path),
                eventType);

            return webHookContent;
        }

        ///  <summary>
        /// Fires the target webhook on the provided content for testing purposes.
        /// </summary>
        /// <snCategory>WebHooks</snCategory>
        /// <param name="webHookContent"></param>
        /// <param name="context"></param>
        /// <param name="nodeId">Target content identifier.</param>
        /// <param name="eventType">Event type to simulate. Can be one of the available events: All, Create, Delete, Modify, Approve,
        /// Reject, Draft, Pending, CheckOut, MoveToTrash, RestoreFromTrash</param>
        /// <returns>The webhook subscription that was fired.</returns>
        [ODataAction(Category = "WebHooks")]
        [ContentTypes("WebHookSubscription")]
        [AllowedRoles(N.R.Administrators, N.R.PublicAdministrators)]
        public static async Task<Content> FireWebHook(Content webHookContent, HttpContext context, int nodeId, WebHookEventType eventType)
        {
            await FireWebHook(context, (WebHookSubscription)webHookContent.ContentHandler, Node.LoadNode(nodeId),
                eventType);

            return webHookContent;
        }

        private static Task FireWebHook(HttpContext context, WebHookSubscription webhook, Node node, WebHookEventType eventType)
        {
            var eventProcessor = context.RequestServices.GetService<IWebHookEventProcessor>();
            return eventProcessor.FireWebHookAsync(webhook, eventType, node, context.RequestAborted);
        }
    }
}

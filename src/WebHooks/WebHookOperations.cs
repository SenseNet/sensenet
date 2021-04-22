using System;
using System.Collections.Generic;
using System.Text;
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
        [ODataAction]
        [ContentTypes("WebHookSubscription")]
        [AllowedRoles(N.R.Administrators)]
        public static async Task<Content> FireWebHook(Content webHookContent, HttpContext context, string path, WebHookEventType eventType)
        {
            await FireWebHook(context, (WebHookSubscription) webHookContent.ContentHandler, Node.LoadNode(path),
                eventType);

            return webHookContent;
        }

        [ODataAction]
        [ContentTypes("WebHookSubscription")]
        [AllowedRoles(N.R.Administrators)]
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

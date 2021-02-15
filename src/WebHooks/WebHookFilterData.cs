using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace SenseNet.WebHooks
{
    /// <summary>
    /// Strongly typed values for the webhook subscription filter
    /// defined on the client in a JSON object.
    /// </summary>
    public class WebHookFilterData
    {
        public string Path { get; set; }
        public bool TriggersForAllEvents { get; set; }
        public ContentTypeFilterData[] ContentTypes { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum WebHookEventType
    {
        All,
        Create,
        Delete,
        Modify,
        Approve,
        Publish,
        Reject,
        CheckIn,
        CheckOut
    }

    public class ContentTypeFilterData
    {
        public string Name { get; set; }

        public WebHookEventType[] Events { get; set; }
    }

    public class WebHookSubscriptionInfo
    {
        public WebHookSubscription Subscription { get; }
        public WebHookEventType EventType { get; }

        public WebHookSubscriptionInfo(WebHookSubscription subscription, WebHookEventType eventType)
        {
            Subscription = subscription;
            EventType = eventType;
        }
    }
}

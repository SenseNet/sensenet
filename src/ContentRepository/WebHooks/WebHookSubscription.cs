using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using SenseNet.Events;

// ReSharper disable InconsistentNaming
namespace SenseNet.WebHooks
{
    /// <summary>
    /// A Content handler that represents a webhook subscription.
    /// </summary>
    [ContentHandler]
    public class WebHookSubscription : GenericContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookSubscription"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public WebHookSubscription(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookSubscription"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public WebHookSubscription(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookSubscription"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected WebHookSubscription(NodeToken nt) : base(nt) { }

        private const string EnabledPropertyName = "Enabled";
        [RepositoryProperty(EnabledPropertyName, RepositoryDataType.Int)]
        public bool Enabled
        {
            get => base.GetProperty<int>(EnabledPropertyName) != 0;
            set => base.SetProperty(EnabledPropertyName, value ? 1 : 0);
        }

        private const string UrlPropertyName = "WebHookUrl";
        [RepositoryProperty(UrlPropertyName, RepositoryDataType.String)]
        public string Url
        {
            get => base.GetProperty<string>(UrlPropertyName);
            set => base.SetProperty(UrlPropertyName, value);
        }

        private const string HttpMethodPropertyName = "WebHookHttpMethod";
        [RepositoryProperty(HttpMethodPropertyName, RepositoryDataType.String)]
        public string HttpMethod
        {
            get => base.GetProperty<string>(HttpMethodPropertyName);
            set => base.SetProperty(HttpMethodPropertyName, value);
        }

        private const string FilterPropertyName = "WebHookFilter";
        [RepositoryProperty(FilterPropertyName, RepositoryDataType.Text)]
        public string Filter
        {
            get => base.GetProperty<string>(FilterPropertyName);
            set => base.SetProperty(FilterPropertyName, value);
        }

        private const string HeadersPropertyName = "WebHookHeaders";
        [RepositoryProperty(HeadersPropertyName, RepositoryDataType.Text)]
        public string Headers
        {
            get => base.GetProperty<string>(HeadersPropertyName);
            set => base.SetProperty(HeadersPropertyName, value);
        }

        private const string HeadersCacheKey = "WebHookHeaders.Key";
        public IDictionary<string, string> HttpHeaders { get; private set; }

        private const string FilterCacheKey = "WebHookFilter.Key";
        public WebHookFilterData FilterData { get; internal set; }

        private const string FilterQueryCacheKey = "WebHookFilterQuery.Key";
        public string FilterQuery { get; set; }

        private const string InvalidFieldsCacheKey = "InvalidFields.Key";
        public string InvalidFields { get; private set; }

        public bool IsValid => string.IsNullOrEmpty(InvalidFields);

        public WebHookEventType? GetRelevantEventType(ISnEvent snEvent)
        {
            WebHookEventType? FindEvent(WebHookEventType eventType)
            {
                if (FilterData?.ContentTypes?.Any(ct => ct.Events.Contains(eventType)) ?? false)
                    return eventType;
                return null;
            }

            //UNDONE: event types are internal, cannot cast sn event
            var eventTypeName = snEvent.GetType().Name;

            switch (eventTypeName)
            {
                case "NodeCreatedEvent":
                    return FindEvent(WebHookEventType.Create);
                case "NodeModifiedEvent":
                    //UNDONE: determine business event type (e.g. Published, Checked in)
                    break;
                case "NodeForcedDeletedEvent":
                    return FindEvent(WebHookEventType.Delete);
                default:
                    return null;
            }

            return null;
        }

        // ===================================================================================== Overrides

        private static readonly Dictionary<string, bool> InvalidFieldNames = new Dictionary<string, bool>
        {
            { FilterPropertyName, false },
            { HeadersPropertyName, false }
        };

        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            var invalidFields = (Dictionary<string, bool>)GetCachedData(InvalidFieldsCacheKey)
                ?? new Dictionary<string, bool>(InvalidFieldNames);

            #region Headers
            HttpHeaders = (IDictionary<string, string>)GetCachedData(HeadersCacheKey);
            if (HttpHeaders == null)
            {
                try
                {
                    HttpHeaders = JsonConvert.DeserializeObject<Dictionary<string, string>>(Headers ?? string.Empty);
                    invalidFields[HeadersPropertyName] = false;
                }
                catch (Exception ex)
                {
                    invalidFields[HeadersPropertyName] = true;
                    SnLog.WriteWarning($"Error parsing webhook headers on subscription {Path}. {ex.Message}");
                }

                if (HttpHeaders == null)
                    HttpHeaders = new Dictionary<string, string>();

                SetCachedData(HeadersCacheKey, HttpHeaders);
            }
            #endregion

            #region Filter data
            FilterData = (WebHookFilterData)GetCachedData(FilterCacheKey);
            if (FilterData == null)
            {
                try
                {
                    FilterData = JsonConvert.DeserializeObject<WebHookFilterData>(Filter ?? string.Empty);
                    invalidFields[FilterPropertyName] = false;
                }
                catch (Exception ex)
                {
                    invalidFields[FilterPropertyName] = true;
                    SnLog.WriteWarning($"Error parsing webhook filters on subscription {Path}. {ex.Message}");
                }

                if (FilterData == null)
                    FilterData = new WebHookFilterData();

                SetCachedData(FilterCacheKey, FilterData);
            }
            #endregion

            #region Filter query

            FilterQuery = (string)GetCachedData(FilterQueryCacheKey);
            if (FilterQuery == null)
            {
                try
                {
                    // subtree filter
                    var queryBuilder = new StringBuilder($"+InTree:'{FilterData.Path ?? "/Root"}'");

                    // add exact type filters
                    if (FilterData?.ContentTypes?.Any() ?? false)
                    {
                        queryBuilder.Append($" +Type:({string.Join(" ", FilterData.ContentTypes.Select(ct => ct.Name))})");
                    }

                    FilterQuery = queryBuilder.ToString();
                }
                catch (Exception ex)
                {
                    invalidFields[FilterPropertyName] = true;
                    SnLog.WriteWarning($"Error building webhook filter query on subscription {Path}. {ex.Message}");
                }

                if (FilterQuery == null)
                    FilterQuery = string.Empty;

                SetCachedData(FilterQueryCacheKey, FilterQuery);
            }

            #endregion

            InvalidFields = string.Join(";", invalidFields.Where(kv => kv.Value).Select(kv => kv.Key));
            SetCachedData(InvalidFieldsCacheKey, invalidFields);
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            return name switch
            {
                EnabledPropertyName => this.Enabled,
                UrlPropertyName => this.Url,
                HttpMethodPropertyName => this.HttpMethod,
                FilterPropertyName => this.Filter,
                HeadersPropertyName => this.Headers,
                _ => base.GetProperty(name),
            };
        }

        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case EnabledPropertyName:
                    this.Enabled = (bool)value;
                    break;
                case UrlPropertyName:
                    this.Url = (string)value;
                    break;
                case HttpMethodPropertyName:
                    this.HttpMethod = (string)value;
                    break;
                case FilterPropertyName:
                    this.Filter = (string)value;
                    break;
                case HeadersPropertyName:
                    this.Headers = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}

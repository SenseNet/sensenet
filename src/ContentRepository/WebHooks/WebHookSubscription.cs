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
        public WebHookFilterData FilterData { get; private set; }

        private const string FilterQueryCacheKey = "WebHookFilterQuery.Key";
        public string FilterQuery { get; set; }

        // ===================================================================================== Overrides

        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            #region Headers
            HttpHeaders = (IDictionary<string, string>)GetCachedData(HeadersCacheKey);
            if (HttpHeaders == null)
            {
                try
                {
                    HttpHeaders = JsonConvert.DeserializeObject<Dictionary<string, string>>(Headers ?? string.Empty);
                }
                catch (Exception ex)
                {
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
                }
                catch (Exception ex)
                {
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
                    if (FilterData.ContentTypes?.Any() ?? false)
                    {
                        queryBuilder.Append($" +Type:({string.Join(" ", FilterData.ContentTypes.Select(ct => ct.Name))})");
                    }

                    FilterQuery = queryBuilder.ToString();
                }
                catch (Exception ex)
                {
                    SnLog.WriteWarning($"Error building webhook filter query on subscription {Path}. {ex.Message}");
                }

                if (FilterQuery == null)
                    FilterQuery = string.Empty;

                SetCachedData(FilterQueryCacheKey, FilterQuery);
            }

            #endregion
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            return name switch
            {
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

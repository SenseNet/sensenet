using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Versioning;
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
        public WebHookFilterData FilterData { get; set; }

        private const string FilterQueryCacheKey = "WebHookFilterQuery.Key";
        public string FilterQuery { get; set; }

        private const string InvalidFieldsPropertyName = "InvalidFields";
        private const string InvalidFieldsCacheKey = "InvalidFields.Key";
        public string InvalidFields { get; private set; }

        private const string IsValidPropertyName = "IsValid";
        public bool IsValid => string.IsNullOrEmpty(InvalidFields);

        private static WebHookEventType[] AllEventTypes { get; } = (WebHookEventType[])Enum.GetValues(typeof(WebHookEventType));

        public WebHookEventType[] GetRelevantEventTypes(ISnEvent snEvent)
        {
            // Check if the subscription contains the type of the content. Currently we
            // treat the defined content types as "exact" types, meaning you have to choose
            // the appropriate type, no type inheritance is taken into account.
            var node = snEvent.NodeEventArgs.SourceNode;
            var contentType = FilterData.ContentTypes.FirstOrDefault(ct => ct.Name == node.NodeType.Name);
            if (contentType == null)
                return Array.Empty<WebHookEventType>();

            // collect events selected by the user
            var selectedEvents = FilterData.TriggersForAllEvents || contentType.Events.Contains(WebHookEventType.All)
                ? AllEventTypes
                : contentType.Events ?? Array.Empty<WebHookEventType>();

            if (!selectedEvents.Any())
                return Array.Empty<WebHookEventType>();

            // collect events based on the selection and versioning state change
            WebHookEventType[] CollectEvents(WebHookEventType firedEvent)
            {
                var list = new List<WebHookEventType>();

                // add the major event (create or modify)
                if (selectedEvents.Contains(firedEvent))
                    list.Add(firedEvent);

                // collect additional possible event
                list.AddRange(CollectVersioningEvents(selectedEvents, snEvent));
                return list.ToArray();
            }

            switch (snEvent)
            {
                case NodeCreatedEvent _:
                    // There are cases when a versioning event should be sent
                    // even if the content is newly created.
                    return CollectEvents(WebHookEventType.Create);
                case NodeModifiedEvent _:
                    return CollectEvents(WebHookEventType.Modify);
                case NodeForcedDeletedEvent _:
                    // Delete means deleted permanently. Delete to Trash should be
                    // a separate event in the future.
                    if (selectedEvents.Contains(WebHookEventType.Delete))
                        return new[] { WebHookEventType.Delete };
                    break;
            }

            return Array.Empty<WebHookEventType>();
        }

        #region Versioning events and states

        /*
        
        These tables show how repo events (e.g. save or publish) affect the versioning state of content items.
        The content starts in the Original state and will convert to the Target state if the versioning and
        approving modes are set as indicated in the last two columns.

        
        Became REJECTED
        ====================================
        Original state	|	Target state	|	repo event		|	Versioning modes	|	Approving mode
        --------------------------------------------------------------------------------------------------
        P				|	R				|	Reject			|	none, major, full	|	ON
        --------------------------------------------------------------------------------------------------


        Became LOCKED
        ====================================
        Original state	|	Target state	|	repo event				|	Versioning modes	|	Approving mode
        ----------------------------------------------------------------------------------------------------------
        anything		|	Locked			|	create, save, checkout	|	none, major, full	|	ON/OFF
        ----------------------------------------------------------------------------------------------------------


        Became DRAFT
        ====================================
        Original state	|	Target state	|	repo event		|	Versioning modes	|	Approving mode
        --------------------------------------------------------------------------------------------------
        A, P, R			|	D				|	create, save	|	full				|	ON/OFF
        --------------------------------------------------------------------------------------------------
        L				|	D				|	checkin			|	full				|	ON/OFF
        --------------------------------------------------------------------------------------------------


        Became PENDING
        ====================================
        Original state	|	Target state	|	repo event		|	Versioning modes	|	Approving mode
        --------------------------------------------------------------------------------------------------
        A, D, R			|	P				|	create, save	|	none, major			|	ON
        --------------------------------------------------------------------------------------------------
        L				|	P				|	checkin			|	none, major			|	ON
        --------------------------------------------------------------------------------------------------
        anything		|	P				|	publish			|	full				|	ON
        --------------------------------------------------------------------------------------------------


        Became APPROVED
        ====================================
        Original state	|	Target state	|	repo event		|	Versioning modes	|	Approving mode
        --------------------------------------------------------------------------------------------------
        D, P, R			|	A				|	create, save	|	none, major			|	OFF
        --------------------------------------------------------------------------------------------------
        L				|	A				|	checkin			|	none, major			|	OFF
        --------------------------------------------------------------------------------------------------
        L				|	A				|	publish			|	full				|	OFF
        --------------------------------------------------------------------------------------------------
        D, R			|	A				|	publish			|	none, major, full	|	OFF
        --------------------------------------------------------------------------------------------------
        P				|	A				|	approve			|	none, major, full	|	ON
        --------------------------------------------------------------------------------------------------

         */

        #endregion

        /// <summary>
        /// Collects events based on the selection and the state change tables above.
        /// </summary>
        private List<WebHookEventType> CollectVersioningEvents(WebHookEventType[] selectedEvents, ISnEvent snEvent)
        {
            var relevantEvents = new List<WebHookEventType>();
            var gc = snEvent.NodeEventArgs.SourceNode as GenericContent;
            var versioningMode = gc?.VersioningMode ?? VersioningType.None;
            var approvingMode = gc?.ApprovingMode ?? ApprovingType.False;
            var eventArgs = snEvent.NodeEventArgs as NodeEventArgs;
            var previousVersion = GetPreviousVersion();
            var currentVersion = snEvent.NodeEventArgs.SourceNode.Version;

            foreach (var eventType in selectedEvents)
            {
                // check whether this event happened
                switch (eventType)
                {
                    case WebHookEventType.Approve:
                        // the content became Approved
                        if (currentVersion.Status == VersionStatus.Approved)
                        {
                            if (approvingMode == ApprovingType.True &&
                                previousVersion?.Status == VersionStatus.Pending ||
                                approvingMode == ApprovingType.False &&
                                ((previousVersion != null && (previousVersion.Status != VersionStatus.Approved ||
                                                              previousVersion < currentVersion)) ||
                                 snEvent is NodeCreatedEvent))
                            {
                                relevantEvents.Add(WebHookEventType.Approve);
                            }
                        }
                        break;
                    case WebHookEventType.Pending:
                        // the content became Pending
                        if (currentVersion.Status == VersionStatus.Pending)
                        {
                            if (approvingMode == ApprovingType.True &&
                                (previousVersion != null && previousVersion.Status != VersionStatus.Pending ||
                                 snEvent is NodeCreatedEvent))
                            {
                                relevantEvents.Add(WebHookEventType.Pending);
                            }
                        }

                        break;
                    case WebHookEventType.Reject:
                        // the content became Rejected
                        if (approvingMode == ApprovingType.True &&
                            previousVersion?.Status == VersionStatus.Pending &&
                            currentVersion.Status == VersionStatus.Rejected)
                        {
                            relevantEvents.Add(WebHookEventType.Reject);
                        }
                        break;
                    case WebHookEventType.Draft:
                        // the content became Draft
                        if (currentVersion.Status == VersionStatus.Draft)
                        {
                            if (versioningMode == VersioningType.MajorAndMinor &&
                                (previousVersion != null && (previousVersion.Status != VersionStatus.Draft ||
                                                             previousVersion < currentVersion) ||
                                 snEvent is NodeCreatedEvent))
                            {
                                relevantEvents.Add(WebHookEventType.Draft);
                            }
                        }
                        break;
                    case WebHookEventType.CheckOut:
                        // the content became Locked
                        if (previousVersion?.Status != VersionStatus.Locked &&
                            currentVersion.Status == VersionStatus.Locked)
                        {
                            relevantEvents.Add(WebHookEventType.CheckOut);
                        }
                        break;
                }
            }

            return relevantEvents.Distinct().ToList();

            VersionNumber GetPreviousVersion()
            {
                var chv = eventArgs?.ChangedData?.FirstOrDefault(cd => cd.Name == "Version");
                if (chv == null)
                    return null;

                return VersionNumber.TryParse((string) chv.Original, out var oldVersion) ? oldVersion : null;
            }
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
            switch (name)
            {
                case EnabledPropertyName: return this.Enabled;
                case IsValidPropertyName: return this.IsValid;
                case InvalidFieldsPropertyName: return this.InvalidFields;
                case UrlPropertyName: return this.Url;
                case HttpMethodPropertyName: return this.HttpMethod;
                case FilterPropertyName: return this.Filter;
                case HeadersPropertyName: return this.Headers;
                default: return base.GetProperty(name);
            }
        }

        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case EnabledPropertyName:
                    this.Enabled = (bool)value;
                    break;
                case IsValidPropertyName:
                case InvalidFieldsPropertyName:
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

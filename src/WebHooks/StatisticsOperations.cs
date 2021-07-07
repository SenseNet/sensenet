using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.Security;
using SafeQueries = SenseNet.ContentRepository.SafeQueries;

namespace SenseNet.WebHooks
{
    public class WebHookAggregation
    {
        public int CallCount { get; set; }
        public int[] StatusCounts { get; set; } = new int[5];
        public long RequestLengths { get; set; }
        public long ResponseLengths { get; set; }
    }
    public class WebHookStatisticalDataAggregator : IStatisticalDataAggregator
    {
        private WebHookAggregation _aggregation = new WebHookAggregation();
        private readonly StatisticsOptions _options;

        public WebHookStatisticalDataAggregator(IOptions<StatisticsOptions> options)
        {
            _options = options.Value;
        }

        public string DataType => "WebHook";
        public bool IsEmpty => _aggregation.CallCount == 0;
        public object Data => _aggregation;
        public AggregationRetentionPeriods RetentionPeriods => _options.Retention.WebHooks;

        public void Aggregate(IStatisticalDataRecord data)
        {
            _aggregation.CallCount++;
            _aggregation.RequestLengths += data.RequestLength ?? 0;
            _aggregation.ResponseLengths += data.ResponseLength ?? 0;
            var leadDigit = (data.ResponseStatusCode ?? 0) / 100 - 1;
            if (leadDigit >= 0 && leadDigit < 5)
                _aggregation.StatusCounts[leadDigit]++;
        }

        public void Summarize(Aggregation[] aggregations)
        {
            foreach (var aggregation in aggregations)
            {
                WebHookAggregation deserialized;
                using (var reader = new StringReader(aggregation.Data))
                    deserialized = JsonSerializer.Create().Deserialize<WebHookAggregation>(new JsonTextReader(reader));
                _aggregation.CallCount += deserialized.CallCount;
                _aggregation.RequestLengths += deserialized.RequestLengths;
                _aggregation.ResponseLengths += deserialized.ResponseLengths;
                var source = deserialized.StatusCounts;
                var target = _aggregation.StatusCounts;
                for (int i = 0; i < source.Length; i++)
                    target[i] += source[i];
            }
        }

        public void Clear()
        {
            _aggregation = new WebHookAggregation();
        }
    }

    public class WebHookUsageViewModel
    {
        private readonly Aggregation[] _timeLine;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly TimeWindow _timeWindow;
        private readonly TimeResolution _resolution;

        public WebHookUsageViewModel(IEnumerable<Aggregation> timeLine, DateTime startTime, DateTime endTime,
            TimeWindow timeWindow, TimeResolution resolution)
        {
            _timeLine = timeLine.ToArray();
            _startTime = startTime;
            _endTime = endTime;
            _timeWindow = timeWindow;
            _resolution = resolution;
        }

        public object GetViewModel()
        {
            WebHookAggregation Deserialize(string src)
            {
                return JsonSerializer.Create()
                    .Deserialize<WebHookAggregation>(new JsonTextReader(new StringReader(src)));
            }

            var count = GetCount();

            var callCount = new int[count];
            var requestLengths = new long[count];
            var responseLengths = new long[count];
            var status100 = new int[count];
            var status200 = new int[count];
            var status300 = new int[count];
            var status400 = new int[count];
            var status500 = new int[count];

            foreach (var item in _timeLine)
            {
                var i = GetIndex(item);
                var data = Deserialize(item.Data);
                callCount[i] = data.CallCount;
                requestLengths[i] = data.RequestLengths;
                responseLengths[i] = data.ResponseLengths;
                status100[i] = data.StatusCounts[0];
                status200[i] = data.StatusCounts[1];
                status300[i] = data.StatusCounts[2];
                status400[i] = data.StatusCounts[3];
                status500[i] = data.StatusCounts[4];
            }

            return new
            {
                DataType = "WebHook",
                Start = _startTime,
                End = _endTime,
                TimeWindow = _timeWindow.ToString(),
                Resolution = _resolution.ToString(),
                CallCount = callCount,
                RequestLengths = requestLengths,
                ResponseLengths = responseLengths,
                Status100 = status100,
                Status200 = status200,
                Status300 = status300,
                Status400 = status400,
                Status500 = status500,
            };
        }
        private int GetCount()
        {
            var period = _endTime - _startTime;
            switch (_resolution)
            {
                case TimeResolution.Minute: return Convert.ToInt32(period.TotalMinutes);
                case TimeResolution.Hour: return Convert.ToInt32(period.TotalHours);
                case TimeResolution.Day: return Convert.ToInt32(period.TotalDays);
                case TimeResolution.Month: return 12 * (_endTime.Year - _startTime.Year) + (_endTime.Month - _startTime.Month);
                default: throw new ArgumentOutOfRangeException();
            }
        }
        private int GetIndex(Aggregation aggregation)
        {
            switch (_resolution)
            {
                case TimeResolution.Minute: return aggregation.Date.Minute;
                case TimeResolution.Hour: return aggregation.Date.Hour;
                case TimeResolution.Day: return aggregation.Date.Day - 1;
                case TimeResolution.Month: return aggregation.Date.Month - 1;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class WebHookUsageListItemViewModel
    {
        public DateTime CreationTime { get; set; }
        public TimeSpan Duration { get; set; }
        public long RequestLength { get; set; }
        public long ResponseLength { get; set; }
        public int ResponseStatusCode { get; set; }
        public string Url { get; set; }
        public int WebHookId { get; set; }
        public int ContentId { get; set; }
        public string EventName { get; set; }
        public string ErrorMessage { get; set; }
        public string Payload { get; set; }

        /// <summary>
        /// For deserializer
        /// </summary>
        public WebHookUsageListItemViewModel() { }
        public WebHookUsageListItemViewModel(IStatisticalDataRecord record)
        {
            var isPermitted = IsPermitted(record.ContentId);

            CreationTime = record.CreationTime ?? DateTime.MinValue;
            Duration = record.Duration ?? TimeSpan.Zero;
            RequestLength = record.RequestLength ?? 0;
            ResponseLength = record.ResponseLength ?? 0;
            ResponseStatusCode = record.ResponseStatusCode ?? 0;
            Url = record.Url;
            WebHookId = record.TargetId ?? 0;
            ContentId = record.ContentId ?? 0;
            EventName = record.EventName;
            ErrorMessage = record.ErrorMessage;
            Payload = isPermitted ? record.GeneralData : null;
        }

        private bool IsPermitted(int? contentId)
        {
            if (contentId == null)
                return true;
            try
            {
                return SecurityHandler.HasPermission(User.Current, contentId.Value, PermissionType.Open);
            }
            catch (EntityNotFoundException)
            {
                return false;
            }
        }
    }

    public static class StatisticsOperations
    {
        [ODataFunction(operationName: "GetWebHookUsageList")]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<IEnumerable<WebHookUsageListItemViewModel>> GetAllWebHookUsageList(Content content, HttpContext httpContext,
          DateTime? maxTime = null, int count = 10)
        {
            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var relatedIds =
                ContentQuery.Query(SafeQueries.TypeIs, QuerySettings.AdminSettings, nameof(WebHookSubscription))
                    .Identifiers.ToArray();

            var records = await dataProvider
                    .LoadUsageListAsync("WebHook", relatedIds, maxTime ?? DateTime.UtcNow, count, httpContext.RequestAborted)
                    .ConfigureAwait(false);

            var items = records
                .Select(x => new WebHookUsageListItemViewModel(x)).ToArray();

            return items;
        }
        [ODataFunction]
        [ContentTypes("WebHookSubscription")]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<IEnumerable<WebHookUsageListItemViewModel>> GetWebHookUsageList(Content content, HttpContext httpContext,
            DateTime? maxTime = null, int count = 10)
        {
            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var records = await dataProvider
                .LoadUsageListAsync("WebHook", new[] {content.Id}, maxTime ?? DateTime.UtcNow, count,
                    httpContext.RequestAborted)
                .ConfigureAwait(false);

            var items = records
                .Select(x => new WebHookUsageListItemViewModel(x)).ToArray();

            return items;
        }

        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static Task<object> GetWebHookUsagePeriods(Content content, HttpContext httpContext,
            TimeWindow? timeWindow = null)
        {
            return GetWebHookUsagePeriods(content, httpContext, DateTime.UtcNow, timeWindow);
        }
        public static async Task<object> GetWebHookUsagePeriods(Content content, HttpContext httpContext, DateTime now,
            TimeWindow? timeWindow = null)
        {
            var window = timeWindow ?? TimeWindow.Month;
            var resolution = (TimeResolution)(int)window;

            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var firstTimes = await dataProvider.LoadFirstAggregationTimesByResolutionsAsync("WebHook", httpContext.RequestAborted)
                .ConfigureAwait(false);

            var firstTime = firstTimes[(int)resolution];

            // Return a default response if the requested window is not represented.
            if (firstTime == null)
                return new
                {
                    Window = window.ToString(),
                    Resolution = resolution.ToString(),
                    First = DateTime.MinValue,
                    Last = DateTime.MinValue,
                    Count = 0
                };

            // Get last item and count in one round.
            var start = now.Truncate(resolution).Truncate(window);
            var last = start;
            var count = 0;
            foreach (var item in EnumeratePeriods(window, start, firstTime.Value))
            {
                last = item;
                count++;
            }

            var result = new
            {
                Window = window.ToString(),
                Resolution = resolution.ToString(),
                First = last,
                Last = start,
                Count = count
            };
            return result;
        }
        private static IEnumerable<DateTime> EnumeratePeriods(TimeWindow window, DateTime start, DateTime firstTime)
        {
            var guard = start;
            while (start >= firstTime)
            {
                yield return start;
                start = start.AddTicks(-1).Truncate(window);

                if (guard == start)
                    throw new InvalidOperationException("Infinite loop in the time-period enumeration.");
                guard = start;
            }
        }

        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<object> GetWebHookUsagePeriod(Content content, HttpContext httpContext,
            TimeWindow? timeWindow = null, DateTime? time = null)
        {
            var window = timeWindow ?? TimeWindow.Month;
            var resolution = (TimeResolution)(int)window;
            var startTime = (time ?? DateTime.UtcNow).Truncate(window);
            var endTime = startTime.Next(window);

            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dbResult = await dataProvider.LoadAggregatedUsageAsync("WebHook", resolution,
                startTime, endTime, httpContext.RequestAborted).ConfigureAwait(false);

            return new WebHookUsageViewModel(dbResult, startTime, endTime, window, resolution).GetViewModel();
        }
    }
}

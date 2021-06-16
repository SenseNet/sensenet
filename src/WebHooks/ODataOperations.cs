using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;

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

        //private StatisticsOptions _options;
        private AggregationRetentionPeriods _retentionPeriods;

        //public WebHookStatisticalDataAggregator(IOptions<StatisticsOptions> options)
        //{
        //    _options = options.Value;
        //}
        public WebHookStatisticalDataAggregator(AggregationRetentionPeriods retentionPeriods)
        {
            _retentionPeriods = retentionPeriods;
        }

        public string DataType => "WebHook";
        public bool IsEmpty => _aggregation.CallCount == 0;
        public object Data => _aggregation;
        //public AggregationRetentionPeriods RetentionPeriods => _options.Retention.WebHooks;
        public AggregationRetentionPeriods RetentionPeriods => _retentionPeriods;

        public void Aggregate(IStatisticalDataRecord data)
        {
            _aggregation.CallCount++;
            _aggregation.RequestLengths += data.RequestLength ?? 0;
            _aggregation.ResponseLengths += data.ResponseLength ?? 0;
            var leadDigit = (data.ResponseStatusCode ?? 0) / 100 - 1;
            if (leadDigit is >= 0 and < 5)
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
    }

    public class WebHookUsageViewModel
    {
        private readonly Aggregation[] _timeLine;
        private DateTime _startTime;
        private DateTime _endTime;
        private TimeWindow _timeWindow;
        private TimeResolution _resolution;

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

            var items = _timeLine.Select(x => Deserialize(x.Data)).ToArray();
            var count = items.Length;

            var callCount = new int[count];
            var requestLengths = new long[count];
            var responseLengths = new long[count];
            var status100 = new int[count];
            var status200 = new int[count];
            var status300 = new int[count];
            var status400 = new int[count];
            var status500 = new int[count];

            for (int i = 0; i < items.Length; i++)
            {
                callCount[i] = items[i].CallCount;
                requestLengths[i] = items[i].RequestLengths;
                responseLengths[i] = items[i].ResponseLengths;
                status100[i] = items[i].StatusCounts[0];
                status200[i] = items[i].StatusCounts[1];
                status300[i] = items[i].StatusCounts[2];
                status400[i] = items[i].StatusCounts[3];
                status500[i] = items[i].StatusCounts[4];
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

        /// <summary>
        /// For deserializer
        /// </summary>
        public WebHookUsageListItemViewModel() { }
        public WebHookUsageListItemViewModel(IStatisticalDataRecord record)
        {
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
        }
    }

    public static class ODataOperations
    {
        [ODataFunction(operationName: "GetWebHookUsageList")]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<IEnumerable<WebHookUsageListItemViewModel>> GetAllWebHookUsageList(Content content, HttpContext httpContext,
          DateTime? maxTime = null, int count = 10)
        {
            //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

            var records = await dataProvider
                    .LoadUsageListAsync("WebHook", maxTime ?? DateTime.UtcNow, count, httpContext.RequestAborted)
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
            //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

            var records = await dataProvider
                    .LoadUsageListAsync("WebHook", content.Id, maxTime ?? DateTime.UtcNow, count, httpContext.RequestAborted)
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

            //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

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

            //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

            var dbResult = await dataProvider.LoadAggregatedUsageAsync("WebHook", resolution,
                startTime, endTime, httpContext.RequestAborted).ConfigureAwait(false);

            return new WebHookUsageViewModel(dbResult, startTime, endTime, window, resolution).GetViewModel();
        }
    }
}

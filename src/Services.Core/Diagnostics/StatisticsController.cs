using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Storage.DataModel.Usage;

namespace SenseNet.Services.Core.Diagnostics
{
    public class ApiUsageViewModel
    {
        private readonly Aggregation[] _timeLine;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly TimeWindow _timeWindow;
        private readonly TimeResolution _resolution;

        public ApiUsageViewModel(IEnumerable<Aggregation> timeLine, DateTime startTime, DateTime endTime,
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
            WebTransferStatisticalDataAggregator.WebTransferAggregation Deserialize(string src)
            {
                return JsonSerializer.Create()
                    .Deserialize<WebTransferStatisticalDataAggregator.WebTransferAggregation>(new JsonTextReader(new StringReader(src)));
            }

            var count = GetCount();

            var callCount = new int[count];
            var requestLengths = new long[count];
            var responseLengths = new long[count];

            foreach(var item in _timeLine)
            {
                var i = GetIndex(item);
                var data = Deserialize(item.Data);
                callCount[i] = data.CallCount;
                requestLengths[i] = data.RequestLengths;
                responseLengths[i] = data.ResponseLengths;
            }

            return new
            {
                DataType = "WebTransfer",
                Start = _startTime,
                End = _endTime,
                TimeWindow = _timeWindow.ToString(),
                Resolution = _resolution.ToString(),
                CallCount = callCount,
                RequestLengths = requestLengths,
                ResponseLengths = responseLengths,
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
                case TimeResolution.Day: return aggregation.Date.Day -1;
                case TimeResolution.Month: return aggregation.Date.Month - 1;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    public class DatabaseUsageViewModel : StatisticsViewModelBase<DatabaseUsage>
    {
        private readonly DatabaseUsage[]_dbUsages;

        public DatabaseUsageViewModel(IEnumerable<Aggregation> timeLine, DateTime startTime, DateTime endTime, 
            TimeWindow timeWindow, TimeResolution resolution) : 
            base(timeLine, startTime, endTime, timeWindow, resolution)
        {
            var count = GetCount();
            _dbUsages = new DatabaseUsage[count];
        }

        protected override void ProcessDataItem(int index, DatabaseUsage data)
        {
            _dbUsages[index] = data;
        }

        protected override object GetResult()
        {
            return new
            {
                DataType = "DatabaseUsage",
                Start = StartTime,
                End = EndTime,
                TimeWindow = TimeWindow.ToString(),
                Resolution = Resolution.ToString(),
                DatabaseUsage = _dbUsages
            };
        }
    }

    public abstract class StatisticsViewModelBase<T>
    {
        protected readonly Aggregation[] TimeLine;
        protected readonly DateTime StartTime;
        protected readonly DateTime EndTime;
        protected readonly TimeWindow TimeWindow;
        protected readonly TimeResolution Resolution;

        protected StatisticsViewModelBase(IEnumerable<Aggregation> timeLine, DateTime startTime, DateTime endTime,
            TimeWindow timeWindow, TimeResolution resolution)
        {
            TimeLine = timeLine.ToArray();
            StartTime = startTime;
            EndTime = endTime;
            TimeWindow = timeWindow;
            Resolution = resolution;
        }

        protected abstract void ProcessDataItem(int index, T data);
        protected abstract object GetResult();

        public object GetViewModel()
        {
            T Deserialize(string src)
            {
                return JsonConvert.DeserializeObject<T>(src);
            }
            
            foreach (var item in TimeLine)
            {
                var i = GetIndex(item);
                var data = Deserialize(item.Data);

                ProcessDataItem(i, data);
            }

            return GetResult();
        }

        protected int GetCount()
        {
            var period = EndTime - StartTime;
            return Resolution switch
            {
                TimeResolution.Minute => Convert.ToInt32(period.TotalMinutes),
                TimeResolution.Hour => Convert.ToInt32(period.TotalHours),
                TimeResolution.Day => Convert.ToInt32(period.TotalDays),
                TimeResolution.Month => 12 * (EndTime.Year - StartTime.Year) + (EndTime.Month - StartTime.Month),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        protected int GetIndex(Aggregation aggregation)
        {
            return Resolution switch
            {
                TimeResolution.Minute => aggregation.Date.Minute,
                TimeResolution.Hour => aggregation.Date.Hour,
                TimeResolution.Day => aggregation.Date.Day - 1,
                TimeResolution.Month => aggregation.Date.Month - 1,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public class ApiUsageListItemViewModel
    {
        public DateTime CreationTime { get; set; }
        public TimeSpan Duration { get; set; }
        public long RequestLength { get; set; }
        public long ResponseLength { get; set; }
        public int ResponseStatusCode { get; set; }
        public string Url { get; set; }

        /// <summary>
        /// For deserializer
        /// </summary>
        public ApiUsageListItemViewModel() { }
        public ApiUsageListItemViewModel(IStatisticalDataRecord record)
        {
            CreationTime = record.CreationTime ?? DateTime.MinValue;
            Duration = record.Duration ?? TimeSpan.Zero;
            RequestLength = record.RequestLength ?? 0;
            ResponseLength = record.ResponseLength ?? 0;
            ResponseStatusCode = record.ResponseStatusCode ?? 0;
            Url = record.Url;
        }
    }


    public static class StatisticsController
    {
        /// <summary>
        /// Gets a list of HTTP requests received by the repository before the provided time.
        /// </summary>
        /// <snCategory>Tools</snCategory>
        /// <remarks>
        /// This action was designed to aid a paging client that needs to display only a handful of records on screen.
        /// Querying for a longer period or for many records may result in a slower response.
        /// </remarks>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <param name="maxTime">The maximum date boundary of the query. Only records before this date
        /// will be returned. Default: current time.</param>
        /// <param name="count">Maximum number of records to load. Default: 10.</param>
        /// <returns>A list of api usage records.</returns>
        [ODataFunction(operationName: "GetApiUsageList")]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<IEnumerable<ApiUsageListItemViewModel>> GetApiUsageList(Content content, HttpContext httpContext,
          DateTime? maxTime = null, int count = 10)
        {
            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var records = await dataProvider
                    .LoadUsageListAsync("WebTransfer", null, maxTime ?? DateTime.UtcNow, count, httpContext.RequestAborted)
                    .ConfigureAwait(false);

            var items = records
                .Select(x => new ApiUsageListItemViewModel(x)).ToArray();

            return items;
        }

        /// <summary>
        /// Gets the availability of HTTP request statistics by time window
        /// and the number of corresponding data points.
        /// </summary>
        /// <snCategory>Tools</snCategory>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <param name="timeWindow">Size of the time window: Hour, Day, Month or Year. Default: Month.</param>
        /// <returns>An API usage statistical data containing start and end dates and count of records.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static Task<object> GetApiUsagePeriods(Content content, HttpContext httpContext,
            TimeWindow? timeWindow = null)
        {
            return GetApiUsagePeriods(content, httpContext, DateTime.UtcNow, timeWindow);
        }
        public static async Task<object> GetApiUsagePeriods(Content content, HttpContext httpContext, DateTime now,
            TimeWindow? timeWindow = null)
        {
            var window = timeWindow ?? TimeWindow.Month;
            var resolution = (TimeResolution)(int)window;

            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var firstTimes = await dataProvider.LoadFirstAggregationTimesByResolutionsAsync("WebTransfer", httpContext.RequestAborted)
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

        /// <summary>
        /// Gets aggregated HTTP requests statistical data in the provided time window.
        /// </summary>
        /// <snCategory>Tools</snCategory>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <param name="timeWindow">Size of the time window: Hour, Day, Month or Year. Default: Month.</param>
        /// <param name="time">Start time. Default: now.</param>
        /// <returns>API usage data containing time window information and a list of aggregated data points.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<object> GetApiUsagePeriod(Content content, HttpContext httpContext,
            TimeWindow? timeWindow = null, DateTime? time = null)
        {
            var window = timeWindow ?? TimeWindow.Month;
            var resolution = (TimeResolution)(int)window;
            var startTime = (time ?? DateTime.UtcNow).Truncate(window);
            var endTime = startTime.Next(window);

            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dbResult = await dataProvider.LoadAggregatedUsageAsync("WebTransfer", resolution,
                startTime, endTime, httpContext.RequestAborted).ConfigureAwait(false);

            return new ApiUsageViewModel(dbResult, startTime, endTime, window, resolution).GetViewModel();
        }

        /// <summary>
        /// Gets aggregated database usage statistical data in the provided time window.
        /// </summary>
        /// <snCategory>Tools</snCategory>
        /// <param name="content"></param>
        /// <param name="httpContext"></param>
        /// <param name="timeWindow">Size of the time window: Hour, Day, Month or Year. Default: Month.</param>
        /// <param name="time">Start time. Default: now.</param>
        /// <returns>Database usage data containing time window information and a list of aggregated data points.</returns>
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<object> GeDatabaseUsagePeriod(Content content, HttpContext httpContext,
            TimeWindow? timeWindow = null, DateTime? time = null)
        {
            var window = timeWindow ?? TimeWindow.Month;
            var resolution = (TimeResolution)(int)window;
            var startTime = (time ?? DateTime.UtcNow).Truncate(window);
            var endTime = startTime.Next(window);

            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dbResult = await dataProvider.LoadAggregatedUsageAsync("DatabaseUsage", resolution,
                startTime, endTime, httpContext.RequestAborted).ConfigureAwait(false);

            return new DatabaseUsageViewModel(dbResult, startTime, endTime, window, resolution).GetViewModel();
        }
    }
}

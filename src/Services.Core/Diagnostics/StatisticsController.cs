﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SafeQueries = SenseNet.ContentRepository.SafeQueries;

namespace SenseNet.Services.Core.Diagnostics
{
    public class ApiUsageViewModel
    {
        private readonly Aggregation[] _timeLine;
        private DateTime _startTime;
        private DateTime _endTime;
        private TimeWindow _timeWindow;
        private TimeResolution _resolution;

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
    }
}

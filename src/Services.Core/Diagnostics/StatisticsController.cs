using System;
using System.Collections.Generic;
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

namespace SenseNet.Services.Core.Diagnostics
{
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
            WebHookStatisticalDataAggregator.WebHookAggregation Deserialize(string src)
            {
                return JsonSerializer.Create()
                    .Deserialize<WebHookStatisticalDataAggregator.WebHookAggregation>(new JsonTextReader(new StringReader(src)));
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
            WebHookId = record.WebHookId ?? 0;
            ContentId = record.ContentId ?? 0;
            EventName = record.EventName;
            ErrorMessage = record.ErrorMessage;
        }
    }

    public static class StatisticsController
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<IEnumerable<WebHookUsageListItemViewModel>> GetWebHookUsageList(Content content, HttpContext httpContext,
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
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static async Task<IEnumerable<DateTime>> GetWebHookUsagePeriods(Content content, HttpContext httpContext,
            TimeWindow? timeWindow = null)
        {
            throw new NotImplementedException();//UNDONE:<?Stat: Implement GetWebHookUsagePeriods
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
                startTime, endTime, httpContext.RequestAborted);

            return new WebHookUsageViewModel(dbResult, startTime, endTime, window, resolution).GetViewModel();
        }
    }
}

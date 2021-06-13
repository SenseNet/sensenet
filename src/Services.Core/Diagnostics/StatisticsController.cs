using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    public class WebHookUsageViewModel
    {
        private IEnumerable<Aggregation> _timeLine;
        public WebHookUsageViewModel(IEnumerable<Aggregation> timeLine)
        {
            _timeLine = timeLine;
        }

        public object GetViewModel()
        {
            throw new NotImplementedException();//UNDONE:<?Stat: Implement WebHookUsageViewModel.GetViewModel
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
            string dataType, TimeWindow? timeWindow = null, DateTime? time = null)
        {
            var window = timeWindow ?? TimeWindow.Month;
            var resolution = (TimeResolution)(int)window;
            var startTime = (time ?? DateTime.UtcNow).Truncate(resolution);
            var endTime = startTime.Next(window);

            var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
            var dbResult = await dataProvider.LoadAggregatedUsageAsync(dataType, resolution, startTime, endTime,
                httpContext.RequestAborted);

            return new WebHookUsageViewModel(dbResult).GetViewModel();
        }
    }
}

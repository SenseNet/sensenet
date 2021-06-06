using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
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
    public static class StatisticsController
    {
        [ODataFunction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static IEnumerable<object> GetWebHookUsageList(Content content, string dataType, DateTime maxTime, int count)
        {
            throw new NotImplementedException();//UNDONE:<?Stat: Implement GetUsageList
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

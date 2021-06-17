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
    public static class StatisticsController
    {
        //[ODataFunction(operationName: "GetWebHookUsageList")]
        //[ContentTypes(N.CT.PortalRoot)]
        //[AllowedRoles(N.R.Administrators, N.R.Developers)]
        //public static async Task<IEnumerable<WebHookUsageListItemViewModel>> GetAllWebHookUsageList(Content content, HttpContext httpContext,
        //    DateTime? maxTime = null, int count = 10)
        //{
        //    //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
        //    var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

        //    var records = await dataProvider
        //            .LoadUsageListAsync("WebHook", maxTime ?? DateTime.UtcNow, count, httpContext.RequestAborted)
        //            .ConfigureAwait(false);

        //    var items = records
        //        .Select(x => new WebHookUsageListItemViewModel(x)).ToArray();

        //    return items;
        //}
        //[ODataFunction]
        //[ContentTypes("WebHookSubscription")]
        //[AllowedRoles(N.R.Administrators, N.R.Developers)]
        //public static async Task<IEnumerable<WebHookUsageListItemViewModel>> GetWebHookUsageList(Content content, HttpContext httpContext,
        //    DateTime? maxTime = null, int count = 10)
        //{
        //    //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
        //    var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

        //    var records = await dataProvider
        //            .LoadUsageListAsync("WebHook", content.Id, maxTime ?? DateTime.UtcNow, count, httpContext.RequestAborted)
        //            .ConfigureAwait(false);

        //    var items = records
        //        .Select(x => new WebHookUsageListItemViewModel(x)).ToArray();

        //    return items;
        //}

        //[ODataFunction]
        //[ContentTypes(N.CT.PortalRoot)]
        //[AllowedRoles(N.R.Administrators, N.R.Developers)]
        //public static async Task<object> GetWebHookUsagePeriods(Content content, HttpContext httpContext,
        //    TimeWindow? timeWindow = null)
        //{
        //    var window = timeWindow ?? TimeWindow.Month;
        //    var resolution = (TimeResolution)(int)window;

        //    //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
        //    var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

        //    var firstTimes = await dataProvider.LoadFirstAggregationTimesByResolutionsAsync("WebHook", httpContext.RequestAborted)
        //        .ConfigureAwait(false);

        //    var firstTime = firstTimes[(int) resolution];

        //    // Return a default response if the requested window is not represented.
        //    if (firstTime == null)
        //        return new
        //        {
        //            Window = window.ToString(),
        //            Resolution = resolution.ToString(),
        //            First = DateTime.MinValue,
        //            Last = DateTime.MinValue,
        //            Count = 0
        //        };

        //    // Get last item and count in one round.
        //    var start = DateTime.UtcNow.Truncate(resolution).Truncate(window);
        //    var last = start;
        //    var count = 0;
        //    foreach (var item in EnumeratePeriods(window, start, firstTime.Value))
        //    {
        //        last = item;
        //        count++;
        //    }

        //    var result = new
        //    {
        //        Window = window.ToString(),
        //        Resolution = resolution.ToString(),
        //        First = last,
        //        Last = start,
        //        Count = count
        //    };
        //    return result;
        //}
        //private static IEnumerable<DateTime> EnumeratePeriods(TimeWindow window, DateTime start, DateTime firstTime)
        //{
        //    var guard = start;
        //    while (start >= firstTime)
        //    {
        //        yield return start;
        //        start = start.AddTicks(-1).Truncate(window);

        //        if (guard == start)
        //            throw new InvalidOperationException("Infinite loop in the time-period enumeration.");
        //        guard = start;
        //    }
        //}

        //[ODataFunction]
        //[ContentTypes(N.CT.PortalRoot)]
        //[AllowedRoles(N.R.Administrators, N.R.Developers)]
        //public static async Task<object> GetWebHookUsagePeriod(Content content, HttpContext httpContext,
        //    TimeWindow? timeWindow = null, DateTime? time = null)
        //{
        //    var window = timeWindow ?? TimeWindow.Month;
        //    var resolution = (TimeResolution)(int)window;
        //    var startTime = (time ?? DateTime.UtcNow).Truncate(window);
        //    var endTime = startTime.Next(window);

        //    //var dataProvider = httpContext.RequestServices.GetRequiredService<IStatisticalDataProvider>();
        //    var dataProvider = Providers.Instance.DataProvider.GetExtension<IStatisticalDataProvider>();

        //    var dbResult = await dataProvider.LoadAggregatedUsageAsync("WebHook", resolution,
        //        startTime, endTime, httpContext.RequestAborted).ConfigureAwait(false);

        //    return new WebHookUsageViewModel(dbResult, startTime, endTime, window, resolution).GetViewModel();
        //}

        /**/

        //UNDONE:<?Stat: TASK: implement Task<IEnumerable<ApiUsageListItemViewModel>> GetApiUsageList
        //UNDONE:<?Stat: TASK: implement Task<object> GetApiUsagePeriods
        //UNDONE:<?Stat: TASK: implement Task<object> GetApiUsagePeriod

        //public static async Task<IEnumerable<ApiUsageListItemViewModel>> GetApiUsageList
        //public static async Task<object> GetApiUsagePeriods
        //public static async Task<object> GetApiUsagePeriod
    }
}

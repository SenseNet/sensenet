using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Options;
using SenseNet.BackgroundOperations;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core.Diagnostics
{
    class StatisticalDataAggregationMaintenanceTask : IMaintenanceTask
    {
        private readonly IStatisticalDataAggregationController _aggregationController;

        public StatisticalDataAggregationMaintenanceTask(IStatisticalDataAggregationController aggregationController)
        {
            _aggregationController = aggregationController;
        }

        public int WaitingSeconds => 1;

        public System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancel)
        {
            //var now = ???;

            //await aggregator.AggregateAsync(aggregationTime, TimeResolution.Minute, cancel);
            //if (now.Minute == 0)
            //{
            //    await aggregator.AggregateAsync(aggregationTime, TimeResolution.Hour, cancel);
            //    if (now.Hour == 0)
            //    {
            //        await aggregator.AggregateAsync(aggregationTime, TimeResolution.Day, cancel);
            //        if (now.Day == 1)
            //        {
            //            await aggregator.AggregateAsync(aggregationTime, TimeResolution.Month, cancel);
            //        }
            //    }
            //}

            throw new System.NotImplementedException(); //UNDONE:<?Stat: Implement StatisticalDataAggregationMaintenanceTask.ExecuteAsync
        }
    }
}

﻿using System;
using System.Threading;
using SenseNet.BackgroundOperations;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Packaging.Steps.Internal;

namespace SenseNet.ContentRepository.Packaging.Steps.Internal
{
    internal class ReindexBinariesTask : IMaintenanceTask
    {
        private bool? _enabled;
        private DateTime _timeLimit;

        public int WaitingSeconds { get; set; } = 9;

        // ReSharper disable once InconsistentNaming
        private SnTrace.SnTraceCategory __tracer;
        internal SnTrace.SnTraceCategory Tracer
        {
            get
            {
                if (__tracer == null)
                {
                    __tracer = SnTrace.Category(ReindexBinaries.TraceCategory);
                    __tracer.Enabled = true;
                }
                return __tracer;
            }
        }

        public System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //UNDONE:DI: Get dependencies from ctor.
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SnCrMsSql"];
            var connectionStrings = new ConnectionStringOptions {Repository = connectionString.ConnectionString};
            var dataHandler = new ReindexBinariesDataHandler(new DataOptions(), connectionStrings);
            var taskHandler = new ReindexBinariesTaskManager(dataHandler);

            if (_enabled == null)
            {
                _enabled = taskHandler.IsFeatureActive();
                if (!_enabled.Value)
                    WaitingSeconds = 600000;
                else
                    _timeLimit = taskHandler.GetTimeLimit();
            }
            if (!_enabled.Value)
                return System.Threading.Tasks.Task.CompletedTask;

            var finished = taskHandler.GetBackgroundTasksAndExecute(_timeLimit);
            if (finished)
            {
                Tracer.Write("All binaries are re-indexed.");
                taskHandler.InactivateFeature();
                Tracer.Write("ReindexBinaries feature is destroyed.");
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}

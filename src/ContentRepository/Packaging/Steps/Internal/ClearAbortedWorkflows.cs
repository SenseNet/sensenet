﻿using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Packaging.Steps.Internal
{
    public class ClearAbortedWorkflows : Step
    {
        public static readonly string WorkflowInstanceClearedGuid = Guid.Empty.ToString();

        public override string ElementName
        {
            get { return "Internal." + this.GetType().Name; }
        }

        public override void Execute(ExecutionContext context)
        {
            using (new SystemAccount())
            {
                var result = ContentQuery.QueryAsync(SafeQueries.ConnectedAbortedAndCompletedWorkflows, QuerySettings.AdminSettings,
                    CancellationToken.None, WorkflowInstanceClearedGuid).ConfigureAwait(false).GetAwaiter().GetResult();

                foreach (var item in result.Nodes)
                {
                    item["WorkflowInstanceGuid"] = WorkflowInstanceClearedGuid;
                    item.Save();
                }

                if (result.Count < 1)
                    Logger.LogMessage("No workflow to clear.");
                else
                    Logger.LogMessage(String.Format("{0} workflows cleared.", result.Count));
            }
        }
    }
}

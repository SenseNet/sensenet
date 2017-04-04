﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging.Steps;

namespace SenseNet.Packaging.Steps
{
    [Annotation("Exports content from repository to filesystem or network path.")]
    public class Export: Step
    {
        [DefaultProperty]
        [Annotation("Repository path of the content to be exported")]
        public string Source { get; set; }

        [Annotation("Filesystem or network path of the folder to export the contents to")]
        public string Target { get; set; }

        [Annotation("Filter for the given source path")]
        public string Filter { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var source = (context.ResolveVariable(Source) as string) ?? "/Root";
            var target = context.ResolveVariable(Target) as string;
            var filter = context.ResolveVariable(Filter) as string;

            context.AssertRepositoryStarted();
            try
            {
                string contextPath = null, queryString = null;
                if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(filter))
                {
                    throw new InvalidStepParameterException("Missing Source or Filter argument.");
                }
                if (!string.IsNullOrWhiteSpace(source))
                {
                    contextPath = ResolveRepositoryPath(source, context);
                    if (RepositoryPath.IsValidPath(contextPath) != RepositoryPath.PathResult.Correct)
                    {
                        throw new InvalidStepParameterException("Invalid repository path.");
                    }
                }
                else
                {
                    contextPath = "/Root";
                }
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    queryString = (string) context.ResolveVariable(filter);
                }

                var targetFolder = ResolveTargetPath(target, context, TargetPathRelativeTo.AppData);
                var savedMode = RepositoryEnvironment.WorkingMode.Exporting;
                RepositoryEnvironment.WorkingMode.SetExporting(true);

                try
                {
                    Exporter.Export(contextPath, targetFolder, queryString);
                }
                catch (Exception e)
                {
                    Logger.LogMessage(@"Export ends with error:\n");
                    Logger.LogMessage(e + @"\n");
                    Logger.LogMessage(e.StackTrace + @"\n");
                }
                finally
                {
                    RepositoryEnvironment.WorkingMode.SetExporting(savedMode);
                }
                
            }
            catch (InvalidStepParameterException)
            {
                Logger.LogMessage("Export step can work with valid paths only.");
                throw;
            }
        }
    }
}

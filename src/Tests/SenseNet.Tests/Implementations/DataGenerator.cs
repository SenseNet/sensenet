using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Packaging;
using SenseNet.Packaging.Steps;

namespace SenseNet.Tests.Implementations
{
    internal class DataGenerator : TestBase
    {
        private static readonly StringBuilder _log = new StringBuilder();
        private class Importlogger : IPackagingLogger
        {
            public LogLevel AcceptedLevel { get; private set; }
            public string LogFilePath { get; private set; }
            public void Initialize(LogLevel level, string logFilePath)
            {
                AcceptedLevel = level;
                LogFilePath = logFilePath;
            }
            public void WriteTitle(string title)
            {
                _log.AppendLine("==============================");
                _log.AppendLine(title);
                _log.AppendLine("==============================");
            }
            public void WriteMessage(string message)
            {
                _log.AppendLine(message);
            }
        }

        public void Generate()
        {
            var sb = new StringBuilder();

            Test(() =>
            {
                if (!(DataProvider.Current is InMemoryDataProvider inmemDb))
                    throw new ApplicationException("Cannot reset the database.");
                inmemDb.ResetDatabase();

                if (!(SearchManager.SearchEngine.IndexingEngine is InMemoryIndexingEngine inmemIndxEngine))
                    throw new ApplicationException("Cannot reset the index.");
                inmemIndxEngine.ClearIndex();

                ContentTypeManager.Reset();

                var importer = new Import
                {
                    ResetSecurity = true,
                    Source = "import",
                    SourceIsRelativeTo = Step.PathRelativeTo.Package,
                    Target = "/Root"
                };

                var packagePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..\\..\\..\\..\\nuget\\snadmin\\install-services"));
                string targetPath = null;
                var networkTargets = new string[0];
                string sandboxPath = null;
                Manifest manifest = null;
                var phase = 1;
                var countOfPhases = 1;
                var parameters = new string[0];
                var console = new StringWriter(sb);
                var executionContext = ExecutionContext.CreateForTest(packagePath, targetPath, networkTargets,
                    sandboxPath, manifest, phase, countOfPhases, parameters, console);

                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);
                Logger.Create(LogLevel.Default, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"importlog{DateTime.UtcNow:yyyyMMdd-HHmmss}.log"));

                executionContext.RepositoryStarted = true;
                RepositoryEnvironment.WorkingMode.SnAdmin = true;

                try
                {
                    importer.Execute(executionContext);
                    RebuildIndex();
                }
                catch
                {
                    throw;
                }
            });
        }

    }
}

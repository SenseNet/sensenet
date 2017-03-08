using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SenseNet.Packaging.Steps
{
	internal class EventLogManager
	{
        internal static readonly string DEFAULT_LOG_NAME = "SenseNet";
        internal static readonly string DEFAULT_MACHINE_NAME = ".";
        internal static readonly string DEFAULT_SOURCES = "Common,SenseNet.ContentRepository,SenseNet.Storage,SenseNet.Portal,SenseNet.CorePortlets,SenseNet.Services,SenseNet.WebSite";

        private readonly string _machineName = DEFAULT_MACHINE_NAME;

	    public int LogSizeInMegaBytes { get; set; } = 15;
	    public string LogName { get; }
	    public IEnumerable<string> Sources { get; }

	    public EventLogManager(string logName, IEnumerable<string> sources)
		{
			LogName = logName;
			Sources = sources;
		}
		public EventLogManager(string logName, IEnumerable<string> sources, string machineName) : this(logName, sources)
		{
			_machineName = machineName;
		}

		private void DeleteSource(string source)
		{
			var sourceBelongsToLog = EventLog.LogNameFromSourceName(source, _machineName);
			if (sourceBelongsToLog != LogName)
			{
				Logger.LogMessage($"Skipping '{source}' - it belongs to log '{sourceBelongsToLog}' rather than '{LogName}'.");
				return;
			}

			EventLog.DeleteEventSource(source, _machineName);
		}
        internal void Delete()
		{
			foreach (var source in Sources)
			{
				DeleteSource(source);
			}

            var builtInSourceName = string.Concat(LogName, "Instrumentation");
            if (!Sources.Contains(builtInSourceName))
				DeleteSource(builtInSourceName);

            if (EventLog.Exists(LogName, _machineName))
            {
                EventLog.Delete(LogName, _machineName);
                Logger.LogMessage($"Log deleted: {LogName}.");
            }
            else
            {
                Logger.LogMessage($"Log does not exist: {LogName}.");
            }
		}

		private void CreateSource(string source)
		{
			var sourceBelongsToLog = EventLog.LogNameFromSourceName(source, _machineName);
			if (sourceBelongsToLog != string.Empty)
			{
                Logger.LogMessage($"Skipping '{source}' - this source already belongs to log '{sourceBelongsToLog}'.");
				return;
			}

            EventLog.CreateEventSource(new EventSourceCreationData(source, LogName) { MachineName = _machineName, });
		}
        internal void Create()
		{
			foreach (var source in Sources)
			{
				CreateSource(source);
			}

            var builtInSourceName = string.Concat(LogName, "Instrumentation");
            if (!Sources.Contains(builtInSourceName))
				CreateSource(builtInSourceName);

			using (var eventLog = new EventLog(LogName, _machineName, builtInSourceName))
			{
				eventLog.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 14);
				eventLog.MaximumKilobytes = LogSizeInMegaBytes * 1024;
				eventLog.WriteEntry("Log created.", EventLogEntryType.Information);
				eventLog.Dispose();

                Logger.LogMessage($"Log created: {LogName}.");
			}
		}
	}
}
using System;

namespace SenseNet.Packaging.Steps
{
    public abstract class EventLogBase : Step
    {
        [Annotation("Name of the log (e.g. MyApplication). Default: SenseNet.")]
        public string LogName { get; set; } = EventLogManager.DEFAULT_LOG_NAME;

        [Annotation("On which machine you want to create the log. Default: .")]
        public string Machine { get; set; } = EventLogManager.DEFAULT_MACHINE_NAME;

        [Annotation("Comma separated names of the sources that will be registered for the log.")]
        public string Sources { get; set; } = EventLogManager.DEFAULT_SOURCES;
    }

    public class CreateEventLog : EventLogBase
    {
        public override void Execute(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(LogName))
                throw new PackagingException("LogName parameter is missing.");

            var eventLogManager = new EventLogManager(LogName, Sources.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries), Machine);
            eventLogManager.Create();
        }
    }

    public class DeleteEventLog : EventLogBase
    {
        public override void Execute(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(LogName))
                throw new PackagingException("LogName parameter is missing.");

            var eventLogManager = new EventLogManager(LogName, Sources.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries), Machine);
            eventLogManager.Delete();
        }
    }
}
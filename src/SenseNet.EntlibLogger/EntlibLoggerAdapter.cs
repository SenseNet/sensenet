using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public class EntLibLoggerAdapter : IEventLogger
    {
        public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title, IDictionary<string, object> properties)
        {
            var props = properties ?? new Dictionary<string, object>();

            var eventTypeName = severity.ToString().ToUpper();

            if (SnTrace.Event.Enabled)
            {
                if (severity <= TraceEventType.Information) // Critical = 1, Error = 2, Warning = 4, Information = 8
                {
                    var id = "#" + Guid.NewGuid().ToString();
                    props["SnTrace"] = id;
                    SnTrace.Event.Write("{0} {1}: {2}", eventTypeName, id, message);
                }
                else
                {
                    object id = "-";
                    object path = "-";
                    if (properties != null)
                    {
                        properties.TryGetValue("Id", out id);
                        properties.TryGetValue("Path", out path);
                    }

                    if (categories.Count == 1 && categories.First() == "Audit")
                        eventTypeName = "Audit";
                    SnTrace.Event.Write("{0}: {1}, Id:{2}, Path:{3}", eventTypeName, message, id, path);
                }
            }

            Microsoft.Practices.EnterpriseLibrary.Logging.Logger.Write(
                message ?? string.Empty, categories, priority, eventId, severity, title ?? string.Empty, props);
        }

        public void Write<T>(object message, ICollection<string> categories, int priority, int eventId,
            TraceEventType severity, string title, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            var properties = getPropertiesCallback != null
                                 ? getPropertiesCallback(callbackArg)
                                 : callbackArg as IDictionary<string, object>;

            Write(message, categories, priority, eventId, severity, title, properties);
        }
    }
}

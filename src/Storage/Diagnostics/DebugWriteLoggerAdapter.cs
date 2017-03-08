using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.Diagnostics
{
    public class DebugWriteLoggerAdapter: IEventLogger
    {
        public void Write(object message, ICollection<string> categories, int priority, int eventId, 
            TraceEventType severity, string title, IDictionary<string, object> properties)
        {
            var props = Utility.CollectAutoProperties(properties);
            string msg = string.Format(@"Message: {0}; Categories:{1}; Priority:{2}; EventId:{3}; " +
                "Severity: {4}; Title: {5}; Properties: {6}",
                message, string.Join(",", categories.ToArray()), priority, eventId, severity, title,
                props == null ? "" : String.Join(", ",
                    (from item in props select String.Concat(item.Key, ":", item.Value)).ToArray()));

            Debug.WriteLine(msg);
        }

        public void Write<T>(object message, ICollection<string> categories, int priority, int eventId,
            TraceEventType severity, string title, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            Write(message, categories, priority, eventId, severity, title,
                getPropertiesCallback(callbackArg));
        }

    }
}

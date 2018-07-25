using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public class DebugWriteLoggerAdapter: IEventLogger
    {
        public void Write(object message, ICollection<string> categories, int priority, int eventId, 
            TraceEventType severity, string title, IDictionary<string, object> properties)
        {
            var msg = $@"Message: {message}; Categories:{
                    string.Join(",", categories.ToArray())
                }; Priority:{priority}; EventId:{eventId}; " +
                $@"Severity: {severity}; Title: {title}; Properties: {(properties == null ? "" :
                    string.Join(", ", (
                        from item in properties select string.Concat(item.Key, ":", item.Value)).ToArray()))}";

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

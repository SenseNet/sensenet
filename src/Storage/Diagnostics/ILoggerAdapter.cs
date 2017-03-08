using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.Diagnostics
{
    public interface ILoggerAdapter
    {
        void Write(object message, ICollection<string> categories, int priority, int eventId, 
            TraceEventType severity, string title, IDictionary<string, object> properties);

        void Write<T>(object message, ICollection<string> categories, int priority, int eventId,
            TraceEventType severity, string title, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg);

    }
}

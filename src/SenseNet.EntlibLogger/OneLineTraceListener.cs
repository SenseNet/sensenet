using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    [ConfigurationElementType(typeof(CustomTraceListenerData))]
    public class OneLineTraceListener : CustomTraceListener
    {
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            if (data is LogEntry entry && Formatter != null)
            {
                WriteLine(Formatter.Format(entry));
            }
            else
            {
                WriteLine(data.ToString());
            }
        }
        public override void Write(string message)
        {
            Debug.Write(message);
        }
        public override void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
